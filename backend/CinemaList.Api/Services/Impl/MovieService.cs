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

namespace CinemaList.Api.Services.Impl;

public class MovieService(RadarrClient radarrClient, OmdbClient omdbClient, IOptions<OMDbSettings> omdbSettings, IOptions<RadarrSettings> radarrSettings, IFilmRepository filmRepository): IMovieService
{
    private readonly RadarrClient _radarrClient = radarrClient;

    private readonly OmdbClient _omdbClient = omdbClient;

    private readonly IFilmRepository _filmRepository = filmRepository;

    private readonly OMDbSettings _omDbSettings = omdbSettings.Value;
    
    private readonly RadarrSettings _radarrSettings = radarrSettings.Value;
    
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
        
        OmdbMovie? omdbMovie = await GetImdbIdFromScrapedFilm(scrapedFilm, cancellationToken);
        if(omdbMovie is null) return null;
        
        RadarrMovie? radarrMovie = await FetchRadarrMetadata(omdbMovie.ImdbId, cancellationToken);
        if (radarrMovie is null) return null;

        bool isInRadarr = await IsFilmInRadarrAsync(radarrMovie.TmdbId.ToString(), cancellationToken);

        return new Film()
        {
            Title = radarrMovie.Title,
            ImdbId = omdbMovie.ImdbId,
            TmdbId = radarrMovie.TmdbId.ToString(),
            IsInRadarr = isInRadarr,
            PosterUrl = radarrMovie.Images.FirstOrDefault(p => p.CoverType == "poster")?.RemoteUrl ?? string.Empty,
            Year = scrapedFilm.Year,
            Country = scrapedFilm.Country
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

    private async Task<OmdbMovie?> GetImdbIdFromScrapedFilm(ScrapedFilm scrapedFilm, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _omdbClient.Client.GetAsync($"?apikey={_omDbSettings.ApiKey}&s={HttpUtility.HtmlEncode(scrapedFilm.Title)}&y={scrapedFilm.Year}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        
        OmdbResponse? omdbResponse = await OmdbResponse.CreateFromResponse(response, cancellationToken);
            
        if (omdbResponse is { Response: "True", Search.Count: > 0 })
        {
            return omdbResponse.Search.FirstOrDefault(x => x.Title.Equals(scrapedFilm.Title));
        }

        return null;
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