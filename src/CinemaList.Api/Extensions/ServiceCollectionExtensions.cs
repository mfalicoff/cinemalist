using System;
using CinemaList.Api.HostedServices;
using CinemaList.Api.Settings;
using CinemaList.Api.Services;
using CinemaList.Api.Services.Impl;
using CinemaList.Scraper.Models;
using CinemaList.Scraper.Repositories;
using CinemaList.Scraper.Repositories.Impl;
using CinemaList.Scraper.Scrapers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CinemaList.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScraping(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        
        services.AddHostedService<ScraperInitializerService>();
        services.AddScoped<IScraperService, ScraperService>();
        services.AddHttpClient<Scraper.Scrapers.Scraper, CinemaModerneScraper>(options =>
        {
            options.BaseAddress = new Uri("https://www.cinemamoderne.com");
        });

        services.AddSingleton<IMongoClient>(sp =>
        {
            MongoDbSettings mongoSettings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(mongoSettings.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            MongoDbSettings mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            IMongoClient mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
            return mongoClient.GetDatabase(mongoSettings.DatabaseName);
        });

        services.AddSingleton<IMongoCollection<ScraperHistoryEntity>>(serviceProvider =>
        {
            MongoDbSettings mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            IMongoClient mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
            return mongoClient
                .GetDatabase(mongoSettings.DatabaseName)
                .GetCollection<ScraperHistoryEntity>("scraper_history");
        });

        services.AddTransient<IScraperHistoryRepository, ScraperFilmRepository>();
        
        return services;
    }
    
    public static IServiceCollection AddMovieServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OMDbSettings>(configuration.GetSection("OMDbSettings"));

        services.AddHttpClient<IIMBDService, IMBDService>(options =>
        {
            options.BaseAddress = new Uri("https://www.omdbapi.com/");
        });
        
        return services;
    }
}