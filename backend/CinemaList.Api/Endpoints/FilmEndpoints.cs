using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Api.Repository;
using CinemaList.Api.Services;
using CinemaList.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CinemaList.Api.Endpoints;

public record FilmResult
{
    public List<Film> Result { get; set; } = [];
}

public static class FilmEndpoints
{
    public static void MapFilmEndpoints(WebApplication app)
    {
        app.MapGroup("/api/films").WithTags("Films").MapEndpoints();
    }

    private static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/all", GetAllFilms)
            .WithName("GetAllFilms")
            .WithDescription("Get all films from the data store.");

        app.MapGet("/{id}", GetFilmById)
            .WithName("GetFilmById")
            .WithDescription("Get a film by its ID from the data store.");

        app.MapPost("/radarr/{id}", AddToRadarr)
            .WithName("AddFilmToRadarr")
            .WithDescription("Add a film to Radarr by its ID.");
    }

    private static async Task<Results<Ok<FilmResult>, ProblemHttpResult>> GetAllFilms(
        IFilmRepository filmRepository
    )
    {
        List<Film> films = await filmRepository.GetAllFilms();
        return TypedResults.Ok(new FilmResult { Result = films });
    }

    private static async Task<Results<Ok<FilmResult>, ProblemHttpResult>> GetFilmById(
        string id,
        IFilmRepository filmRepository
    )
    {
        Film film = await filmRepository.GetFilmById(id);
        return TypedResults.Ok(new FilmResult { Result = [film] });
    }

    private static async Task<Results<Ok, ProblemHttpResult>> AddToRadarr(
        string id,
        IMovieService movieService
    )
    {
        await movieService.AddMovieToRadarr(id, CancellationToken.None);
        return TypedResults.Ok();
    }
}
