using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Api.Repository;
using CinemaList.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CinemaList.Api.Services.Impl;

public class ScraperService(
    IEnumerable<Scraper.Scrapers.Scraper> scrapers,
    IMovieService movieService,
    IFilmRepository filmRepository,
    IMemoryCache memoryCache,
    ILogger<ScraperService> logger
) : IScraperService
{
    private readonly IEnumerable<Scraper.Scrapers.Scraper> _scrapers = scrapers;
    private readonly IMovieService _movieService = movieService;
    private readonly IFilmRepository _filmRepository = filmRepository;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<ScraperService> _logger = logger;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                object? filmTitle = context.GetValueOrDefault("FilmTitle", "Unknown");
                logger.LogWarning(
                    exception,
                    "Retry {RetryCount} for film {Title} after {Delay}s",
                    retryCount,
                    filmTitle,
                    timeSpan.TotalSeconds
                );
            }
        );

    public async Task ScrapeFilmsAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _scrapers.Select(async scraper =>
        {
            if (!await scraper.ShouldRunScraper(cancellationToken))
            {
                return;
            }

            List<ScrapedFilm> films = await scraper.Scrape(cancellationToken);
            List<Film> enrichedFilms = [];

            foreach (var film in films)
            {
                Film? enrichedFilm = await _retryPolicy.ExecuteAsync(
                    async (ctx) => await _movieService.FetchMovieMetadata(film, ctx),
                    cancellationToken
                );

                if (enrichedFilm != null)
                    enrichedFilms.Add(enrichedFilm);
            }

            await Task.WhenAll(_filmRepository.UpsertFilms(enrichedFilms, cancellationToken));
            await scraper.PersistRunAsync(enrichedFilms, cancellationToken);
        });

        await Task.WhenAll(tasks);
    }
}
