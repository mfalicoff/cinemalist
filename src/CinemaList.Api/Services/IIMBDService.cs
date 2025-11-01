using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;

namespace CinemaList.Api.Services;

public interface IIMBDService
{
    public Task<OmdbMovie?> GetSearchMovie(ScrapedFilm scrapedFilm, CancellationToken cancellationToken = default);
}