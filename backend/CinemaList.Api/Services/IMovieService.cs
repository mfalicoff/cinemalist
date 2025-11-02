using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;

namespace CinemaList.Api.Services;

public interface IMovieService
{
    /// <summary>
    /// Completes movie metadata from omdb and then additional information from radarr.
    /// </summary>
    /// <param name="scrapedFilm"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Film?> FetchMovieMetadata(ScrapedFilm scrapedFilm, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Adds movie to radarr by the given TMDB id.
    /// </summary>
    /// <param name="tmdbId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddMovieToRadarr(string tmdbId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update movie status with radarr if a change occured through another source
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task SynchronizeWithRadarr(CancellationToken stoppingToken);
}