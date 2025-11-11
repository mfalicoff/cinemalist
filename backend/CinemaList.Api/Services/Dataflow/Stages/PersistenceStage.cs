using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CinemaList.Api.Repository;
using CinemaList.Api.Services.Dataflow.Models;
using CinemaList.Common.Models;
using Microsoft.Extensions.Logging;

namespace CinemaList.Api.Services.Dataflow.Stages;

/// <summary>
/// Pipeline stage that persists enriched films to the database.
/// Handles batch persistence of films and updates scraper history.
/// </summary>
public class PersistenceStage(
    IEnumerable<Scraper.Scrapers.Scraper> scrapers,
    IFilmRepository filmRepository,
    PipelineMetrics metrics,
    ILogger<PersistenceStage> logger
)
{
    private readonly IEnumerable<Scraper.Scrapers.Scraper> _scrapers =
        scrapers ?? throw new ArgumentNullException(nameof(scrapers));
    private readonly IFilmRepository _filmRepository =
        filmRepository ?? throw new ArgumentNullException(nameof(filmRepository));
    private readonly PipelineMetrics _metrics =
        metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILogger<PersistenceStage> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    // Track all films per scraper for final persistence
    private readonly Dictionary<string, List<Film>> _filmsByScraperName = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Creates a batch block that groups enriched films for efficient persistence.
    /// </summary>
    public BatchBlock<EnrichedFilm> CreateBatchBlock(
        int batchSize,
        int boundedCapacity,
        CancellationToken cancellationToken
    )
    {
        return new BatchBlock<EnrichedFilm>(
            batchSize,
            new GroupingDataflowBlockOptions
            {
                BoundedCapacity = boundedCapacity,
                CancellationToken = cancellationToken,
            }
        );
    }

    /// <summary>
    /// Creates an action block that persists batches of films to the database.
    /// </summary>
    public ActionBlock<EnrichedFilm[]> CreatePersistBlock(
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken
    )
    {
        return new ActionBlock<EnrichedFilm[]>(
            async enrichedFilms => await PersistBatchAsync(enrichedFilms, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            }
        );
    }

    /// <summary>
    /// Persists a batch of enriched films to the database.
    /// </summary>
    private async Task PersistBatchAsync(
        EnrichedFilm[] enrichedFilms,
        CancellationToken cancellationToken
    )
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            // Convert enriched films to film batch with statistics
            FilmBatch filmBatch = CreateFilmBatch(enrichedFilms);

            if (filmBatch.Films.Count == 0)
            {
                _logger.LogInformation("Batch contained no valid films to persist");
                return;
            }

            _logger.LogInformation(
                "Persisting batch of {Count} films (Total: {Total}, Cached: {Cached}, Failed: {Failed})",
                filmBatch.SuccessCount,
                filmBatch.TotalAttempted,
                filmBatch.CachedCount,
                filmBatch.FailureCount
            );

            await PersistFilmsAndHistoryAsync(enrichedFilms, filmBatch.Films, cancellationToken);

            // Update metrics
            Interlocked.Add(ref _metrics.FilmsPersisted, filmBatch.Films.Count);

            _logger.LogInformation(
                "Successfully persisted batch of {Count} films",
                filmBatch.Films.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting film batch");
        }
    }

    /// <summary>
    /// Creates a FilmBatch record from an array of enriched films.
    /// </summary>
    private static FilmBatch CreateFilmBatch(EnrichedFilm[] enrichedFilms)
    {
        List<Film> validFilms = enrichedFilms
            .Where(ef => ef.Film != null)
            .Select(ef => ef.Film!)
            .ToList();

        int successCount = enrichedFilms.Count(ef => ef.Status == EnrichmentStatus.Success);
        int cachedCount = enrichedFilms.Count(ef => ef.Status == EnrichmentStatus.CachedSuccess);
        int failureCount = enrichedFilms.Count(ef =>
            ef.Status == EnrichmentStatus.OmdbFailure || ef.Status == EnrichmentStatus.RadarrFailure
        );

        return new FilmBatch(
            Films: validFilms,
            TotalAttempted: enrichedFilms.Length,
            SuccessCount: successCount,
            FailureCount: failureCount,
            CachedCount: cachedCount
        );
    }

    /// <summary>
    /// Persists films to the repository and accumulates films per scraper for final history update.
    /// </summary>
    private async Task PersistFilmsAndHistoryAsync(
        EnrichedFilm[] enrichedFilms,
        IReadOnlyList<Film> films,
        CancellationToken cancellationToken
    )
    {
        // Accumulate films by scraper name for final persistence (deduplicated by TmdbId or Title+Year)
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            foreach (EnrichedFilm enrichedFilm in enrichedFilms.Where(ef => ef.Film != null))
            {
                if (!_filmsByScraperName.ContainsKey(enrichedFilm.ScraperName))
                {
                    _filmsByScraperName[enrichedFilm.ScraperName] = [];
                }

                // Only add if not already present (deduplicate by TmdbId if available, otherwise by Title)
                Film film = enrichedFilm.Film!;
                bool isDuplicate = !string.IsNullOrEmpty(film.TmdbId)
                    ? _filmsByScraperName[enrichedFilm.ScraperName]
                        .Any(f => f.TmdbId == film.TmdbId)
                    : _filmsByScraperName[enrichedFilm.ScraperName]
                        .Any(f => f.Title == film.Title && f.Year == film.Year);

                if (!isDuplicate)
                {
                    _filmsByScraperName[enrichedFilm.ScraperName].Add(film);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        // Upsert films to repository
        await Task.WhenAll(_filmRepository.UpsertFilms(films.ToList(), cancellationToken));
    }

    /// <summary>
    /// Finalizes the persistence by updating scraper history once per scraper with all accumulated films.
    /// Should be called after all batches have been processed.
    /// </summary>
    public async Task FinalizeScraperHistoryAsync(CancellationToken cancellationToken = default)
    {
        List<Task> persistTasks = [];

        // Update scraper history for each scraper with all its accumulated films
        foreach (Scraper.Scrapers.Scraper scraper in _scrapers)
        {
            string scraperName = scraper.GetType().Name;
            if (
                _filmsByScraperName.TryGetValue(scraperName, out List<Film>? scraperFilms)
                && scraperFilms.Count > 0
            )
            {
                _logger.LogInformation(
                    "Finalizing scraper history for {ScraperName} with {Count} films",
                    scraperName,
                    scraperFilms.Count
                );

                persistTasks.Add(scraper.PersistRunAsync(scraperFilms, cancellationToken));
            }
        }

        await Task.WhenAll(persistTasks);

        _logger.LogInformation("Scraper history finalized for all scrapers");
    }
}
