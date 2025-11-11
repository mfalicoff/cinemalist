using System;
using System.Collections.Generic;
using CinemaList.Common.Models;

namespace CinemaList.Api.Services.Dataflow.Models;

/// <summary>
/// Represents a film that has been scraped from a cinema website.
/// Includes the original scraped film data.
/// </summary>
public record ScrapedFilmItem(ScrapedFilm Film, string ScraperName, DateTime ScrapedAt);

/// <summary>
/// Represents a film after deduplication processing.
/// Includes a unique cache key for tracking duplicates.
/// </summary>
public record DeduplicatedFilm(
    ScrapedFilm Film,
    string CacheKey,
    bool IsDuplicate,
    string ScraperName
);

/// <summary>
/// Represents a film enriched with OMDB and Radarr metadata.
/// May be null if enrichment failed.
/// </summary>
public record EnrichedFilm(
    Film? Film,
    ScrapedFilm OriginalScrapedFilm,
    string CacheKey,
    bool WasCached,
    string ScraperName,
    EnrichmentStatus Status
);

/// <summary>
/// Status of the film enrichment process.
/// </summary>
public enum EnrichmentStatus
{
    Success,
    CachedSuccess,
    OmdbFailure,
    RadarrFailure,
    Skipped,
}

/// <summary>
/// Represents a batch of films ready for persistence.
/// </summary>
public record FilmBatch(
    IReadOnlyList<Film> Films,
    int TotalAttempted,
    int SuccessCount,
    int FailureCount,
    int CachedCount
);

/// <summary>
/// Performance metrics for the scraping pipeline.
/// </summary>
public record PipelineMetrics
{
    public int TotalScraped;
    public int DuplicatesFiltered;
    public int CacheHits;
    public int OmdbCalls;
    public int OmdbFailures;
    public int FilmsPersisted;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; set; }

    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public double ThroughputPerSecond =>
        Duration.TotalSeconds > 0 ? FilmsPersisted / Duration.TotalSeconds : 0;
    public double CacheHitRate =>
        OmdbCalls + CacheHits > 0 ? CacheHits / (double)(OmdbCalls + CacheHits) : 0;
    public double DuplicationRate =>
        TotalScraped > 0 ? DuplicatesFiltered / (double)TotalScraped : 0;
    public double OmdbFailureRate => OmdbCalls > 0 ? OmdbFailures / (double)OmdbCalls : 0;
}
