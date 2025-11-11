using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CinemaList.Api.Services.Dataflow.Models;
using Microsoft.Extensions.Logging;

namespace CinemaList.Api.Services.Dataflow.Stages;

/// <summary>
/// Pipeline stage that deduplicates films based on title and year.
/// Filters out duplicate films to avoid redundant OMDB API calls.
/// </summary>
public class DeduplicationStage(PipelineMetrics metrics, ILogger<DeduplicationStage> logger)
{
    private readonly PipelineMetrics _metrics =
        metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILogger<DeduplicationStage> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<string, bool> _seenFilms = new();

    /// <summary>
    /// Creates the deduplication block that filters duplicate films.
    /// </summary>
    public TransformBlock<ScrapedFilmItem, DeduplicatedFilm> CreateBlock(
        int boundedCapacity,
        CancellationToken cancellationToken
    )
    {
        return new TransformBlock<ScrapedFilmItem, DeduplicatedFilm>(
            item => DeduplicateFilm(item),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                BoundedCapacity = boundedCapacity,
                CancellationToken = cancellationToken,
            }
        );
    }

    /// <summary>
    /// Processes a scraped film item and marks it as duplicate if already seen.
    /// </summary>
    private DeduplicatedFilm DeduplicateFilm(ScrapedFilmItem item)
    {
        // Create unique key: normalized title + year
        string cacheKey = GenerateCacheKey(item.Film.Title, item.Film.Year);

        // Check if we've seen this film before
        bool isNew = _seenFilms.TryAdd(cacheKey, true);
        bool isDuplicate = !isNew;

        if (isDuplicate)
        {
            Interlocked.Increment(ref _metrics.DuplicatesFiltered);
            _logger.LogDebug(
                "Duplicate film detected: {Title} ({Year}) from {Scraper}",
                item.Film.Title,
                item.Film.Year,
                item.ScraperName
            );
        }

        return new DeduplicatedFilm(
            Film: item.Film,
            CacheKey: cacheKey,
            IsDuplicate: isDuplicate,
            ScraperName: item.ScraperName
        );
    }

    /// <summary>
    /// Generates a unique cache key for a film based on title and year.
    /// Normalizes the title to handle minor variations.
    /// </summary>
    private static string GenerateCacheKey(string? title, string? year)
    {
        string normalizedTitle = title?.Trim().ToLowerInvariant() ?? string.Empty;
        string normalizedYear = year?.Trim() ?? string.Empty;
        return $"{normalizedTitle}|{normalizedYear}";
    }

    /// <summary>
    /// Clears the seen films cache. Useful for testing or manual cache resets.
    /// </summary>
    public void ClearCache()
    {
        _seenFilms.Clear();
        _logger.LogInformation("Deduplication cache cleared");
    }

    /// <summary>
    /// Gets the current count of unique films seen.
    /// </summary>
    public int UniqueFilmCount => _seenFilms.Count;
}
