using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaList.Scraper.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver;

namespace CinemaList.Api.Endpoints;

public record ScraperResult
{
    public List<ScraperHistoryEntity> Result { get; set; } = [];
}

public static class ScraperEndpoints
{
    public static void MapScraperEndpoints(WebApplication app)
    {
        app.MapGroup("/api/scraper").WithTags("Scraper").MapEndpoints();
    }
    
    private static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/history", GetScraperHistory)
            .WithName("GetScraperHistory")
            .WithDescription("Get scraper history from the data store.");
    }
    
    private static async Task<Results<Ok<ScraperResult>, ProblemHttpResult>> GetScraperHistory(IMongoCollection<ScraperHistoryEntity> scraperHistoryCollection)
    {
        List<ScraperHistoryEntity>? history = await scraperHistoryCollection.Find(_ => true).ToListAsync();
        return TypedResults.Ok(new ScraperResult { Result = history });
    }
}