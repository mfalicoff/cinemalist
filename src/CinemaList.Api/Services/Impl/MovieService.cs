using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CinemaList.Api.Models;
using CinemaList.Api.Repository;
using CinemaList.Api.Settings;
using CinemaList.Common.Models;
using Microsoft.Extensions.Options;

namespace CinemaList.Api.Services.Impl;

public class MovieService(HttpClient httpClient, IOptions<OMDbSettings> settings, IFIlmRepository fIlmRepository): IMovieService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IFIlmRepository _fIlmRepository = fIlmRepository;

    private readonly OMDbSettings _omDbSettings = settings.Value;

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
        HttpClient radarrClient = new HttpClient();
        radarrClient.DefaultRequestHeaders.Add("X-Api-Key", "6fb27e548692416084615b4f2cea48f4");
        var request = new
        {
            tmdbId = tmdbId,
            qualityProfileId = 5,
            rootFolderPath = "/mnt/media/movies",
            monitored = true,
            addOptions = new
            {
                searchForMovie = false
            }
        };

        HttpResponseMessage response = await radarrClient.PostAsJsonAsync($"https://radarr.caddy.mazilious.org/api/v3/movie/", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        // change movie to monitored
        await _fIlmRepository.UpdateFilmRadarrStatus(tmdbId, true, cancellationToken);
        
    }
    
    private async Task<OmdbMovie?> GetImdbIdFromScrapedFilm(ScrapedFilm scrapedFilm, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"?apikey={_omDbSettings.ApiKey}&s={HttpUtility.HtmlEncode(scrapedFilm.Title)}&y={scrapedFilm.Year}", cancellationToken);
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
        HttpClient radarrClient = new HttpClient();
        radarrClient.DefaultRequestHeaders.Add("X-Api-Key", "6fb27e548692416084615b4f2cea48f4");
        HttpResponseMessage response = await radarrClient.GetAsync($"https://radarr.caddy.mazilious.org/api/v3/movie/lookup/imdb?imdbId={imdbId}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        
        string stringResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<RadarrMovie>(stringResponse);
    }
    
    private async Task<bool> IsFilmInRadarrAsync(string tmdbId, CancellationToken cancellationToken)
    {
        HttpClient radarrClient = new HttpClient();
        radarrClient.DefaultRequestHeaders.Add("X-Api-Key", "6fb27e548692416084615b4f2cea48f4");
        HttpResponseMessage response = await radarrClient.GetAsync($"https://radarr.caddy.mazilious.org/api/v3/movie?tmdbId={tmdbId}", cancellationToken);
        if (!response.IsSuccessStatusCode) return false;
        
        string stringResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        List<RadarrMovie>? radarrMovies = JsonSerializer.Deserialize<List<RadarrMovie>>(stringResponse);
        return radarrMovies is { Count: > 0 };
    }
}