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
    public Task<List<Film>> GetAllFilms();
    
    /// <summary>
    /// Insert or update films in the data store.
    /// </summary>
    /// <param name="films"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task UpsertFilms(List<Film> films, CancellationToken cancellationToken = default);
}