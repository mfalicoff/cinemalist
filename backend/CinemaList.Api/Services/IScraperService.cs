using System.Threading;
using System.Threading.Tasks;

namespace CinemaList.Api.Services;

public interface IScraperService
{
    Task ScrapeFilmsAsync(CancellationToken cancellationToken = default);
}
