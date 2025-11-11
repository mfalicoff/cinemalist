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
    ILogger<PersistenceStage> logger)
{
    private readonly IEnumerable<Scraper.Scrapers.Scraper> _scrapers = scrapers ?? throw new ArgumentNullException(nameof(scrapers));
    private readonly IFilmRepository _filmRepository = filmRepository ?? throw new ArgumentNullException(nameof(filmRepository));
    private readonly PipelineMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILogger<PersistenceStage> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Creates a batch block that groups enriched films for efficient persistence.
    /// </summary>
    public BatchBlock<EnrichedFilm> CreateBatchBlock(
        int batchSize,
        int boundedCapacity,
        CancellationToken cancellationToken)
    {
        return new BatchBlock<EnrichedFilm>(batchSize, new GroupingDataflowBlockOptions
        {
            BoundedCapacity = boundedCapacity,
            CancellationToken = cancellationToken
        });
    }

    /// <summary>
    /// Creates an action block that persists batches of films to the database.
    /// </summary>
    public ActionBlock<EnrichedFilm[]> CreatePersistBlock(
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        return new ActionBlock<EnrichedFilm[]>(
            async enrichedFilms => await PersistBatchAsync(enrichedFilms, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            });
    }

    /// <summary>
    /// Persists a batch of enriched films to the database.
    /// </summary>
    private async Task PersistBatchAsync(
        EnrichedFilm[] enrichedFilms,
        CancellationToken cancellationToken)
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
                filmBatch.FailureCount);

            await PersistFilmsAndHistoryAsync(filmBatch.Films, cancellationToken);

            // Update metrics
            Interlocked.Add(ref _metrics.FilmsPersisted, filmBatch.Films.Count);

            _logger.LogInformation("Successfully persisted batch of {Count} films", filmBatch.Films.Count);
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
            ef.Status == EnrichmentStatus.OmdbFailure ||
            ef.Status == EnrichmentStatus.RadarrFailure);

        return new FilmBatch(
            Films: validFilms,
            TotalAttempted: enrichedFilms.Length,
            SuccessCount: successCount,
            FailureCount: failureCount,
            CachedCount: cachedCount
        );
    }

    /// <summary>
    /// Persists films to the repository and updates scraper history in parallel.
    /// </summary>
    private async Task PersistFilmsAndHistoryAsync(
        IReadOnlyList<Film> films,
        CancellationToken cancellationToken)
    {
        List<Task> persistTasks = [];

        // Update scraper history for each scraper
        foreach (Scraper.Scrapers.Scraper scraper in _scrapers)
        {
            persistTasks.Add(scraper.PersistRunAsync(films.ToList(), cancellationToken));
        }

        // Upsert films to repository
        persistTasks.AddRange(_filmRepository.UpsertFilms(films.ToList(), cancellationToken));

        await Task.WhenAll(persistTasks);
    }
}
