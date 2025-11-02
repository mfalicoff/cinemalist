using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;

namespace CinemaList.Api.Services;

public interface IMovieService
{
    Task<Film?> FetchMovieMetadata(ScrapedFilm scrapedFilm, CancellationToken cancellationToken = default);
    
    Task AddMovieToRadarr(string tmdbId, CancellationToken cancellationToken = default);
}