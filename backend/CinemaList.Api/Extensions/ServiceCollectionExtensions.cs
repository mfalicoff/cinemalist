using System;
using CinemaList.Api.Clients;
using CinemaList.Api.HostedServices;
using CinemaList.Api.Repository;
using CinemaList.Api.Repository.Impl;
using CinemaList.Api.Services;
using CinemaList.Api.Services.Impl;
using CinemaList.Api.Settings;
using CinemaList.Common.Models;
using CinemaList.Scraper.Models;
using CinemaList.Scraper.Scrapers;
using Mazilious.Common.Configuration;
using Mazilious.Common.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TMDbLib.Client;

namespace CinemaList.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScraping(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHostedService<ScraperInitializerService>();
        services.AddScoped<IScraperService, ScraperService>();

        services.AddHttpClient<CinemaModerneScraper>(options =>
        {
            options.BaseAddress = new Uri("https://www.cinemamoderne.com");
        });

        services.AddHttpClient<CinemaBeaubienScraper>(options =>
        {
            options.BaseAddress = new Uri("https://cinemacinema.ca");
        });

        services.AddTransient<Scraper.Scrapers.Scraper>(sp =>
            sp.GetRequiredService<CinemaModerneScraper>()
        );
        services.AddTransient<Scraper.Scrapers.Scraper>(sp =>
            sp.GetRequiredService<CinemaBeaubienScraper>()
        );

        MongoBuilder mongo = services.AddMongo(configuration);
        mongo
            .ConfigureMongo()
            .RegisterMongoCollection<ScraperHistoryEntity>("scraper_history")
            .RegisterMongoCollection<Film>("films");

        services.AddTransient<IFilmRepository, FilmRepository>();

        return services;
    }

    public static IServiceCollection AddMovieServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHostedService<RadarrSynchronizerService>();

        TmdbSettings settings = configuration.GetRequiredSection<TmdbSettings>();
        services.AddSingleton<TMDbClient>(_ => new TMDbClient(settings.ApiKey));

        services.AddHttpClient<RadarrClient>(client =>
        {
            RadarrSettings radarrSettings = configuration.GetRequiredSection<RadarrSettings>();
            client.BaseAddress = new Uri(radarrSettings.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("X-Api-Key", radarrSettings.ApiKey);
        });

        services.AddTransient<IMovieService, MovieService>();

        return services;
    }
}
