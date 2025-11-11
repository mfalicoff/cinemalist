using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CinemaList.Api.Clients;
using CinemaList.Api.Models;
using CinemaList.Api.Repository;
using CinemaList.Api.Settings;
using CinemaList.Common.Models;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace CinemaList.Api.Services.Impl;

public class MovieService(RadarrClient radarrClient, TMDbClient tmdbClient, IOptions<RadarrSettings> radarrSettings, IFilmRepository filmRepository, TimeProvider timeProvider): IMovieService
{
    private readonly RadarrClient _radarrClient = radarrClient;

    private readonly TMDbClient _tmDbClient = tmdbClient;

    private readonly IFilmRepository _filmRepository = filmRepository;
    
    private readonly RadarrSettings _radarrSettings = radarrSettings.Value;
    
    private readonly TimeProvider _timeProvider = timeProvider;
    
    private record RadarrRequest
    {
        public required string TmdbId { get; init; }
        public required string QualityProfileId { get; init; }
        public required string RootFolderPath { get; init; }
        public required bool Monitored { get; init; }
        public required RadarrAddOptions AddOptions { get; init; }
    }
    
    private record RadarrAddOptions
    {
        public required bool SearchForMovie { get; init; }
    }


    public async Task<Film?> FetchMovieMetadata(ScrapedFilm scrapedFilm, CancellationToken cancellationToken = default)
    {
        
        SearchMovie? searchMovie = await GetImdbIdFromScrapedFilm(scrapedFilm, cancellationToken);
        if(searchMovie is null) return null;

        bool isInRadarr = await IsFilmInRadarrAsync(searchMovie.Id.ToString(), cancellationToken);

        return new Film()
        {
            Title = searchMovie.Title,
            TmdbId = searchMovie.Id.ToString(),
            IsInRadarr = isInRadarr,
            PosterUrl = $"https://image.tmdb.org/t/p/original{searchMovie.PosterPath}",
            Year = scrapedFilm.Year,
            Country = scrapedFilm.Country,
            ScrapedDate = _timeProvider.GetUtcNow().Date
        };
    }
    
    public async Task AddMovieToRadarr(string tmdbId, CancellationToken cancellationToken = default)
    {
        RadarrRequest request = new()
        {
            TmdbId = tmdbId,
            QualityProfileId = _radarrSettings.QualityProfileId.ToString(),
            RootFolderPath = _radarrSettings.RootFolderPath,
            Monitored = true,
            AddOptions = new RadarrAddOptions
            {
                SearchForMovie = false
            }
        };

        HttpResponseMessage response = await _radarrClient.Client.PostAsJsonAsync("api/v3/movie/", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        await _filmRepository.UpdateFilmRadarrStatus(tmdbId, true, cancellationToken);
        
    }

    public async Task SynchronizeWithRadarr(CancellationToken cancellationToken)
    {
        List<Film> filmsNotInRadarr = await _filmRepository.GetFilmsByFilter(FilmFilter.NotInRadarr, cancellationToken);

        List<Task> tasks = [];
        tasks.AddRange(filmsNotInRadarr.Select(film => ProcessFilmAsync(film, cancellationToken)));
        
        await Task.WhenAll(tasks);
        return;
        
        async Task ProcessFilmAsync(Film film, CancellationToken ct)
        {
            bool isInRadarr = await IsFilmInRadarrAsync(film.TmdbId, ct);
            if (isInRadarr)
            {
                await _filmRepository.UpdateFilmRadarrStatus(film.TmdbId, true, ct);
            }
        }
    }

    private async Task<SearchMovie?> GetImdbIdFromScrapedFilm(ScrapedFilm scrapedFilm, CancellationToken cancellationToken)
    {
        SearchContainer<SearchMovie>? response = await _tmDbClient.SearchMovieAsync(scrapedFilm.Title, year: int.Parse(scrapedFilm.Year ?? "0"), includeAdult: true, cancellationToken: cancellationToken);
            
        return response is { Results.Count: > 0 } ? response.Results.FirstOrDefault(x => x.Title.Equals(scrapedFilm.Title)) : null;
    }
    
    private async Task<RadarrMovie?> FetchRadarrMetadata(string imdbId, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _radarrClient.Client.GetAsync($"api/v3/movie/lookup/imdb?imdbId={imdbId}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        
        string stringResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<RadarrMovie>(stringResponse);
    }
    
    private async Task<bool> IsFilmInRadarrAsync(string tmdbId, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _radarrClient.Client.GetAsync($"api/v3/movie?tmdbId={tmdbId}", cancellationToken);
        if (!response.IsSuccessStatusCode) return false;
        
        string stringResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        List<RadarrMovie>? radarrMovies = JsonSerializer.Deserialize<List<RadarrMovie>>(stringResponse);
        return radarrMovies is { Count: > 0 };
    }
}