using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CinemaList.Api.Configuration;
using CinemaList.Api.Repository;
using CinemaList.Api.Services.Dataflow.Models;
using CinemaList.Api.Services.Dataflow.Stages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CinemaList.Api.Services.Impl;

/// <summary>
/// Refactored TPL Dataflow-based scraper service with clean separation of concerns.
/// Uses dedicated stage classes and record types for better maintainability and testability.
///
/// Pipeline flow:
/// Scraper → Deduplication → OMDB Enrichment → Batch → Persistence
/// </summary>
public class ScraperService(
    IEnumerable<Scraper.Scrapers.Scraper> scrapers,
    IMovieService movieService,
    IFilmRepository filmRepository,
    IMemoryCache memoryCache,
    IOptions<DataflowScraperOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<ScraperService> logger)
    : IScraperService
{
    private readonly IEnumerable<Scraper.Scrapers.Scraper> _scrapers = scrapers ?? throw new ArgumentNullException(nameof(scrapers));
    private readonly IMovieService _movieService = movieService ?? throw new ArgumentNullException(nameof(movieService));
    private readonly IFilmRepository _filmRepository = filmRepository ?? throw new ArgumentNullException(nameof(filmRepository));
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    private readonly DataflowScraperOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<ScraperService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public async Task ScrapeFilmsAsync(CancellationToken cancellationToken = default)
    {
        PipelineMetrics metrics = new()
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting dataflow scraper pipeline at {Time}", metrics.StartTime);

            // Create pipeline stages
            ScraperStage scraperStage = new(
                _scrapers,
                metrics,
                _loggerFactory.CreateLogger<ScraperStage>());

            DeduplicationStage deduplicationStage = new(
                metrics,
                _loggerFactory.CreateLogger<DeduplicationStage>());

            OmdbEnrichmentStage enrichmentStage = new(
                _movieService,
                _memoryCache,
                metrics,
                _options.OmdbRetryCount,
                _options.CacheExpirationHours,
                _options.EnableOmdbCaching,
                _loggerFactory.CreateLogger<OmdbEnrichmentStage>());

            PersistenceStage persistenceStage = new(
                _scrapers,
                _filmRepository,
                metrics,
                _loggerFactory.CreateLogger<PersistenceStage>());

            // Create dataflow blocks
            TransformManyBlock<Scraper.Scrapers.Scraper, ScrapedFilmItem> scraperBlock = scraperStage.CreateBlock(
                _options.MaxScraperParallelism,
                cancellationToken);

            TransformBlock<ScrapedFilmItem, DeduplicatedFilm>? deduplicationBlock = _options.EnableDeduplication
                ? deduplicationStage.CreateBlock(_options.BoundedCapacity, cancellationToken)
                : null;

            TransformBlock<DeduplicatedFilm, EnrichedFilm> enrichmentBlock = enrichmentStage.CreateBlock(
                _options.MaxOmdbParallelism,
                _options.BoundedCapacity,
                cancellationToken);

            BatchBlock<EnrichedFilm> batchBlock = persistenceStage.CreateBatchBlock(
                _options.BatchSize,
                _options.BoundedCapacity,
                cancellationToken);

            ActionBlock<EnrichedFilm[]> persistBlock = persistenceStage.CreatePersistBlock(
                _options.MaxPersistenceParallelism,
                cancellationToken);

            // Link pipeline stages
            DataflowLinkOptions linkOptions = new()
            {
                PropagateCompletion = true
            };

            if (_options.EnableDeduplication && deduplicationBlock != null)
            {
                // Pipeline with deduplication: Scraper → Dedup → Enrichment → Batch → Persist
                scraperBlock.LinkTo(deduplicationBlock, linkOptions);
                deduplicationBlock.LinkTo(enrichmentBlock, linkOptions);
            }
            else
            {
                // Pipeline without deduplication: Scraper → Enrichment → Batch → Persist
                // Create adapter block to convert ScrapedFilmItem to DeduplicatedFilm
                TransformBlock<ScrapedFilmItem, DeduplicatedFilm> adapterBlock = new(
                    item => new DeduplicatedFilm(
                        Film: item.Film,
                        CacheKey: $"{item.Film.Title?.Trim().ToLowerInvariant()}|{item.Film.Year}",
                        IsDuplicate: false,
                        ScraperName: item.ScraperName),
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                        BoundedCapacity = _options.BoundedCapacity,
                        CancellationToken = cancellationToken
                    });

                scraperBlock.LinkTo(adapterBlock, linkOptions);
                adapterBlock.LinkTo(enrichmentBlock, linkOptions);
            }

            enrichmentBlock.LinkTo(batchBlock, linkOptions);
            batchBlock.LinkTo(persistBlock, linkOptions);

            // Handle batch triggering when scraper completes
            _ = scraperBlock.Completion.ContinueWith(_ =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    batchBlock.TriggerBatch();
                }
            }, cancellationToken);

            // Post all scrapers to the pipeline
            foreach (Scraper.Scrapers.Scraper scraper in _scrapers)
            {
                await scraperBlock.SendAsync(scraper, cancellationToken);
            }

            // Signal completion and wait
            scraperBlock.Complete();
            await persistBlock.Completion;

            // Finalize metrics
            metrics.EndTime = DateTime.UtcNow;

            // Log performance metrics if enabled
            if (_options.EnablePerformanceMetrics)
            {
                LogPerformanceMetrics(metrics);
            }

            _logger.LogInformation("Dataflow scraper pipeline completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in dataflow scraper pipeline");
            throw;
        }
    }

    /// <summary>
    /// Logs detailed performance metrics for the scraping pipeline.
    /// </summary>
    private void LogPerformanceMetrics(PipelineMetrics metrics)
    {
        _logger.LogInformation(
            "Dataflow Pipeline Performance Metrics\n" +
            "════════════════════════════════════════\n" +
            "Duration:             {Duration:mm\\:ss}\n" +
            "Films Scraped:        {Scraped}\n" +
            "Duplicates Filtered:  {Duplicates} ({DuplicatePercent:P1})\n" +
            "Cache Hits:           {CacheHits} ({CacheHitRate:P1})\n" +
            "OMDB API Calls:       {OmdbCalls}\n" +
            "OMDB Failures:        {OmdbFailures} ({FailureRate:P1})\n" +
            "Films Persisted:      {Persisted}\n" +
            "Throughput:           {Throughput:F2} films/sec\n" +
            "════════════════════════════════════════",
            metrics.Duration,
            metrics.TotalScraped,
            metrics.DuplicatesFiltered,
            metrics.DuplicationRate,
            metrics.CacheHits,
            metrics.CacheHitRate,
            metrics.OmdbCalls,
            metrics.OmdbFailures,
            metrics.OmdbFailureRate,
            metrics.FilmsPersisted,
            metrics.ThroughputPerSecond);
    }
}
