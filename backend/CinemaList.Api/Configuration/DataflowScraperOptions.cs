namespace CinemaList.Api.Configuration;

/// <summary>
/// Configuration options for the TPL Dataflow-based scraper service.
/// These settings control parallelism, buffering, batching behavior, and optimizations.
/// </summary>
public class DataflowScraperOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "DataflowScraper";

    /// <summary>
    /// Maximum number of parallel OMDB API calls.
    /// Lower values help prevent rate limiting.
    /// Default: 5
    /// </summary>
    public int MaxOmdbParallelism { get; set; } = 5;

    /// <summary>
    /// Number of films to batch together before persisting to the database.
    /// Higher values improve database efficiency but use more memory.
    /// Default: 50
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Maximum number of items that can be buffered in each pipeline stage.
    /// This provides backpressure when downstream stages are slower.
    /// Default: 100
    /// </summary>
    public int BoundedCapacity { get; set; } = 1000;

    /// <summary>
    /// Maximum degree of parallelism for running scrapers.
    /// Set to -1 for unbounded (all scrapers run in parallel).
    /// Default: -1 (unbounded)
    /// </summary>
    public int MaxScraperParallelism { get; set; } = -1;

    /// <summary>
    /// Maximum degree of parallelism for database persistence operations.
    /// Usually should be 1 to serialize writes, but can be increased for performance.
    /// Default: 1
    /// </summary>
    public int MaxPersistenceParallelism { get; set; } = 1;

    /// <summary>
    /// Timeout in seconds for OMDB API calls.
    /// Default: 30
    /// </summary>
    public int OmdbTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout in seconds for scraper operations.
    /// Default: 60
    /// </summary>
    public int ScraperTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Enable OMDB response caching to avoid repeated API calls.
    /// Cached responses expire after CacheExpirationHours.
    /// Default: true
    /// </summary>
    public bool EnableOmdbCaching { get; set; } = true;

    /// <summary>
    /// Number of hours to cache OMDB responses.
    /// Default: 24 hours
    /// </summary>
    public int CacheExpirationHours { get; set; } = 24;

    /// <summary>
    /// Maximum size of the memory cache in MB.
    /// Default: 100 MB
    /// </summary>
    public int CacheSizeLimitMB { get; set; } = 100;

    /// <summary>
    /// Enable deduplication of films across scrapers.
    /// Films with the same title and year will only be processed once.
    /// Default: true
    /// </summary>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary>
    /// Number of retry attempts for failed OMDB API calls.
    /// Uses exponential backoff between retries.
    /// Default: 3
    /// </summary>
    public int OmdbRetryCount { get; set; } = 3;

    /// <summary>
    /// Enable detailed performance metrics logging.
    /// Default: true
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
}
