using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;

namespace CinemaList.Scraper.Scrapers;

/// <summary>
/// Abstract base class for web scrapers that extract film information from cinema websites.
/// </summary>
public abstract class Scraper
{
    /// <summary>
    /// Scrapes film information from the target website.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A list of scraped films containing basic information extracted from the website.</returns>
    public abstract Task<List<ScrapedFilm>> Scrape(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines whether the scraper should run based on the last execution time or other business rules.
    /// Depends on the implementation of the derived scraper class.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>True if the scraper should run; otherwise, false.</returns>
    public abstract Task<bool> ShouldRunScraper(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the scraped and enriched film data to the data store and updates the scraper execution history.
    /// </summary>
    /// <param name="films">The list of fully enriched films to persist.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous persist operation.</returns>
    public abstract Task PersistRunAsync(List<Film> films, CancellationToken cancellationToken = default);
}