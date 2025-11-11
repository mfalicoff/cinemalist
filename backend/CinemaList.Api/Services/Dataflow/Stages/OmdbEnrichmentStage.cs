using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CinemaList.Api.Services.Dataflow.Models;
using CinemaList.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CinemaList.Api.Services.Dataflow.Stages;

/// <summary>
/// Pipeline stage that enriches films with OMDB and Radarr metadata.
/// Uses caching to avoid redundant API calls and retry logic for resilience.
/// </summary>
public class OmdbEnrichmentStage
{
    private readonly IMovieService _movieService;
    private readonly IMemoryCache _memoryCache;
    private readonly PipelineMetrics _metrics;
    private readonly ILogger<OmdbEnrichmentStage> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly int _cacheExpirationHours;
    private readonly bool _cachingEnabled;

    public OmdbEnrichmentStage(
        IMovieService movieService,
        IMemoryCache memoryCache,
        PipelineMetrics metrics,
        int retryCount,
        int cacheExpirationHours,
        bool cachingEnabled,
        ILogger<OmdbEnrichmentStage> logger)
    {
        _movieService = movieService ?? throw new ArgumentNullException(nameof(movieService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheExpirationHours = cacheExpirationHours;
        _cachingEnabled = cachingEnabled;

        // Create retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    object? filmTitle = context.GetValueOrDefault("FilmTitle", "Unknown");
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} for film {Title} after {Delay}s",
                        retryCount, filmTitle, timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// Creates the OMDB enrichment block that enriches films with metadata.
    /// </summary>
    public TransformBlock<DeduplicatedFilm, EnrichedFilm> CreateBlock(
        int maxDegreeOfParallelism,
        int boundedCapacity,
        CancellationToken cancellationToken)
    {
        return new TransformBlock<DeduplicatedFilm, EnrichedFilm>(
            async dedupFilm => await EnrichFilmAsync(dedupFilm, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                BoundedCapacity = boundedCapacity,
                CancellationToken = cancellationToken
            });
    }

    /// <summary>
    /// Enriches a deduplicated film with OMDB and Radarr metadata.
    /// Uses caching and retry logic for better performance and reliability.
    /// </summary>
    private async Task<EnrichedFilm> EnrichFilmAsync(
        DeduplicatedFilm dedupFilm,
        CancellationToken cancellationToken)
    {
        // Skip duplicates - they've already been processed
        if (dedupFilm.IsDuplicate)
        {
            return new EnrichedFilm(
                Film: null,
                OriginalScrapedFilm: dedupFilm.Film,
                CacheKey: dedupFilm.CacheKey,
                WasCached: false,
                ScraperName: dedupFilm.ScraperName,
                Status: EnrichmentStatus.Skipped
            );
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return CreateFailedEnrichment(dedupFilm, EnrichmentStatus.Skipped);
        }

        try
        {
            // Check cache first if caching is enabled
            if (_cachingEnabled && _memoryCache.TryGetValue(dedupFilm.CacheKey, out Film? cachedFilm))
            {
                Interlocked.Increment(ref _metrics.CacheHits);
                _logger.LogDebug("Cache hit for film: {Title} ({Year})",
                    dedupFilm.Film.Title, dedupFilm.Film.Year);

                return new EnrichedFilm(
                    Film: cachedFilm,
                    OriginalScrapedFilm: dedupFilm.Film,
                    CacheKey: dedupFilm.CacheKey,
                    WasCached: true,
                    ScraperName: dedupFilm.ScraperName,
                    Status: EnrichmentStatus.CachedSuccess
                );
            }

            // Not in cache, fetch from OMDB with retry logic
            Interlocked.Increment(ref _metrics.OmdbCalls);

            Context context = new()
            {
                ["FilmTitle"] = dedupFilm.Film.Title ?? "Unknown"
            };

            Film? enrichedFilm = await _retryPolicy.ExecuteAsync(
                async (ctx) => await _movieService.FetchMovieMetadata(dedupFilm.Film, cancellationToken),
                context);

            if (enrichedFilm == null)
            {
                Interlocked.Increment(ref _metrics.OmdbFailures);
                _logger.LogWarning("Could not enrich film: {Title} ({Year})",
                    dedupFilm.Film.Title, dedupFilm.Film.Year);

                return CreateFailedEnrichment(dedupFilm, EnrichmentStatus.OmdbFailure);
            }

            // Cache the successful result
            if (_cachingEnabled)
            {
                MemoryCacheEntryOptions cacheOptions = new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_cacheExpirationHours),
                    Size = 1 // For cache size limit tracking
                };
                _memoryCache.Set(dedupFilm.CacheKey, enrichedFilm, cacheOptions);
            }

            _logger.LogInformation("Enriched film {Title} with IMDB ID {ImdbId}",
                dedupFilm.Film.Title, enrichedFilm.ImdbId);

            return new EnrichedFilm(
                Film: enrichedFilm,
                OriginalScrapedFilm: dedupFilm.Film,
                CacheKey: dedupFilm.CacheKey,
                WasCached: false,
                ScraperName: dedupFilm.ScraperName,
                Status: EnrichmentStatus.Success
            );
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _metrics.OmdbFailures);
            _logger.LogError(ex, "Error enriching film {Title} after retries",
                dedupFilm.Film.Title);

            return CreateFailedEnrichment(dedupFilm, EnrichmentStatus.OmdbFailure);
        }
    }

    /// <summary>
    /// Creates an EnrichedFilm record for a failed enrichment attempt.
    /// </summary>
    private static EnrichedFilm CreateFailedEnrichment(DeduplicatedFilm dedupFilm, EnrichmentStatus status)
    {
        return new EnrichedFilm(
            Film: null,
            OriginalScrapedFilm: dedupFilm.Film,
            CacheKey: dedupFilm.CacheKey,
            WasCached: false,
            ScraperName: dedupFilm.ScraperName,
            Status: status
        );
    }

    /// <summary>
    /// Clears the OMDB response cache. Useful for testing or manual cache invalidation.
    /// </summary>
    public void ClearCache()
    {
        // Note: IMemoryCache doesn't have a built-in Clear() method
        // This would require tracking keys or recreating the cache
        _logger.LogWarning("Cache clear requested but IMemoryCache doesn't support full clear");
    }
}
