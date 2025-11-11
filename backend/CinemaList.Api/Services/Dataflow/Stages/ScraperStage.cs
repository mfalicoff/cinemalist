using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CinemaList.Api.Services.Dataflow.Models;
using CinemaList.Common.Models;
using Microsoft.Extensions.Logging;

namespace CinemaList.Api.Services.Dataflow.Stages;

/// <summary>
/// Pipeline stage that executes scrapers and produces ScrapedFilmItem records.
/// Runs all scrapers in parallel and flattens results into individual film items.
/// </summary>
public class ScraperStage(
    IEnumerable<Scraper.Scrapers.Scraper> scrapers,
    PipelineMetrics metrics,
    ILogger<ScraperStage> logger)
{
    private readonly IEnumerable<Scraper.Scrapers.Scraper> _scrapers = scrapers ?? throw new ArgumentNullException(nameof(scrapers));
    private readonly ILogger<ScraperStage> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly PipelineMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    /// <summary>
    /// Creates the scraper block that runs each scraper and outputs individual film items.
    /// </summary>
    public TransformManyBlock<Scraper.Scrapers.Scraper, ScrapedFilmItem> CreateBlock(
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        return new TransformManyBlock<Scraper.Scrapers.Scraper, ScrapedFilmItem>(
            async scraper => await ExecuteScraperAsync(scraper, cancellationToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                BoundedCapacity = _scrapers.Count(),
                CancellationToken = cancellationToken
            });
    }

    /// <summary>
    /// Executes a single scraper and returns scraped film items.
    /// </summary>
    private async Task<IEnumerable<ScrapedFilmItem>> ExecuteScraperAsync(
        Scraper.Scrapers.Scraper scraper,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        string scraperName = scraper.GetType().Name;

        try
        {
            // Check if scraper should run based on its business rules
            if (!await scraper.ShouldRunScraper(cancellationToken))
            {
                _logger.LogInformation("Skipping scraper {ScraperName} - should not run", scraperName);
                return [];
            }

            _logger.LogInformation("Running scraper: {ScraperName}", scraperName);

            List<ScrapedFilm> scrapedFilms = await scraper.Scrape(cancellationToken);
            DateTime scrapedAt = DateTime.UtcNow;

            // Update metrics
            Interlocked.Add(ref _metrics.TotalScraped, scrapedFilms.Count);

            _logger.LogInformation("Scraped {Count} films from {ScraperName}",
                scrapedFilms.Count, scraperName);

            // Transform into ScrapedFilmItem records
            return scrapedFilms.Select(film => new ScrapedFilmItem(
                Film: film,
                ScraperName: scraperName,
                ScrapedAt: scrapedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running scraper {ScraperName}", scraperName);
            return [];
        }
    }
}
