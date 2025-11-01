using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Api.Repository;
using CinemaList.Common.Models;
using Microsoft.Extensions.Logging;

namespace CinemaList.Api.Services.Impl;

public class ScraperService(IEnumerable<Scraper.Scrapers.Scraper> scrapers, IIMBDService imbdService, IFIlmRepository filmRepository, ILogger<ScraperService> logger): IScraperService
{
    private readonly IEnumerable<Scraper.Scrapers.Scraper> _scrapers = scrapers;

    private readonly IIMBDService _imbdService = imbdService;
    private readonly IFIlmRepository _filmRepository = filmRepository;

    private readonly ILogger<ScraperService> _logger = logger;
    
    public async Task ScrapeFilmsAsync(CancellationToken cancellationToken = default)
    {
        foreach (Scraper.Scrapers.Scraper scraper in _scrapers)
        {
            if (!await scraper.ShouldRunScraper(cancellationToken)) continue;
            
            List<ScrapedFilm> scrapedFilms = await ScrapeFilms(scraper, cancellationToken);
            List<Film> films = await CorrelateWithOmdb(scrapedFilms, cancellationToken);
            await PersistFilmsAsync(films, cancellationToken);
        }
    }

    private async Task<List<ScrapedFilm>> ScrapeFilms(Scraper.Scrapers.Scraper scraper, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        try
        {
            _logger.LogInformation("Running scraper: {ScraperName}", scraper.GetType().Name);
            List<ScrapedFilm> scrapedFilms = await scraper.Scrape(cancellationToken);
            _logger.LogInformation("Scraped {Count} films from {ScraperName}", scrapedFilms.Count, scraper.GetType().Name);
            return scrapedFilms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running scraper {ScraperName}", scraper.GetType().Name);
        }
        
        return [];
    }
    
    private async Task<List<Film>> CorrelateWithOmdb(List<ScrapedFilm> scrapedFilms, CancellationToken cancellationToken)
    {
        List<Film> correlatedFilms = [];

        foreach (ScrapedFilm scrapedFilm in scrapedFilms)
        {
            try
            {
                OmdbMovie? omdbMovie = await _imbdService.GetSearchMovie(scrapedFilm, cancellationToken);
                
                if (omdbMovie == null) continue;
                
                Film film = Film.FromScrapedFilmAndIMDBId(scrapedFilm, omdbMovie);
                correlatedFilms.Add(film);
                _logger.LogInformation("Correlated film {FilmTitle} with OMDB ID {OmdbID}", scrapedFilm.Title, omdbMovie.imdbID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error correlating film {FilmTitle} with OMDB", scrapedFilm.Title);
            }
        }
        return correlatedFilms;
    }
    
    private async Task PersistFilmsAsync(List<Film> films, CancellationToken cancellationToken)
    {
        List<Task> persistTasks = [];
        persistTasks.AddRange(_scrapers.Select(scraper => scraper.PersistRunAsync(films, cancellationToken)));
        persistTasks.AddRange(_filmRepository.UpsertFilms(films, cancellationToken));

        await Task.WhenAll(persistTasks);
    }
}