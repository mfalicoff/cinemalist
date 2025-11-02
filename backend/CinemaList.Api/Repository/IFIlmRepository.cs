using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;

namespace CinemaList.Api.Repository;

public interface IFIlmRepository
{
    /// <summary>
    /// Get all films from the data store.
    /// </summary>
    /// <returns></returns>
    Task<List<Film>> GetAllFilms();
    
    /// <summary>
    /// Insert or update films in the data store.
    /// </summary>
    /// <param name="films"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task UpsertFilms(List<Film> films, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update the Radarr status of a film.
    /// </summary>
    /// <param name="tmdbId"></param>
    /// <param name="isInRadarr"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task UpdateFilmRadarrStatus(string tmdbId, bool isInRadarr, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a film by its ID from the data store.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Film> GetFilmById(string id, CancellationToken cancellationToken = default);
}