using System;
using CinemaList.Api.Clients;
using CinemaList.Api.HostedServices;
using CinemaList.Api.Repository;
using CinemaList.Api.Repository.Impl;
using CinemaList.Api.Settings;
using CinemaList.Api.Services;
using CinemaList.Api.Services.Impl;
using CinemaList.Common.Models;
using CinemaList.Scraper.Models;
using CinemaList.Scraper.Scrapers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CinemaList.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScraping(this IServiceCollection services)
    {
        services.AddHostedService<ScraperInitializerService>();
        services.AddScoped<IScraperService, ScraperService>();
        services.AddHttpClient<Scraper.Scrapers.Scraper, CinemaModerneScraper>(options =>
        {
            options.BaseAddress = new Uri("https://www.cinemamoderne.com");
        });

        services.AddSingleton<IMongoClient>(sp =>
        {
            MongoDbSettings mongoSettings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            if (string.IsNullOrEmpty(mongoSettings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "MongoDB connection string not found. Configure it in MongoDbSettings:ConnectionString or store it in Vault as 'ConnectionString'.");
            }
            
            return new MongoClient(mongoSettings.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            MongoDbSettings mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            IMongoClient mongoClient = serviceProvider.GetRequiredService<IMongoClient>();

            if (string.IsNullOrEmpty(mongoSettings.DatabaseName))
            {
                throw new InvalidOperationException(
                    "MongoDB database name not found. Configure it in MongoDbSettings:DatabaseName or store it in Vault as 'DatabaseName'.");
            }
            
            return mongoClient.GetDatabase(mongoSettings.DatabaseName);
        });

        services.AddSingleton<IMongoCollection<ScraperHistoryEntity>>(serviceProvider =>
        {
            IMongoDatabase mongoDatabase = serviceProvider.GetRequiredService<IMongoDatabase>();
            return mongoDatabase.GetCollection<ScraperHistoryEntity>("scraper_history");
        });
        
        services.AddSingleton<IMongoCollection<Film>>(serviceProvider =>
        {
            IMongoDatabase mongoDatabase = serviceProvider.GetRequiredService<IMongoDatabase>();
            return mongoDatabase.GetCollection<Film>("films");
        });
        services.AddTransient<IFilmRepository, FilmRepository>();
        
        return services;
    }
    
    public static IServiceCollection AddMovieServices(this IServiceCollection services)
    {
        services.AddHttpClient<OmdbClient>( client =>
        {
            client.BaseAddress = new Uri("https://www.omdbapi.com/");
        });
        
        services.AddHttpClient<RadarrClient>(client =>
        {
            RadarrSettings radarrSettings =
                services.BuildServiceProvider().GetRequiredService<IOptions<RadarrSettings>>().Value;
            client.BaseAddress = new Uri(radarrSettings.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("X-Api-Key", radarrSettings.ApiKey);
        });
        
        services.AddTransient<IMovieService, MovieService>();
        
        return services;
    }
}