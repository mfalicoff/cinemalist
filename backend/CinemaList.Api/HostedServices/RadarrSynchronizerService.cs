using System;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaList.Api.HostedServices;

public class RadarrSynchronizerService(
    IMovieService movieService,
    ILogger<RadarrSynchronizerService> logger
) : BackgroundService
{
    private readonly IMovieService _movieService = movieService;

    private readonly ILogger<RadarrSynchronizerService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _movieService.SynchronizeWithRadarr(stoppingToken);

        using PeriodicTimer timer = new(TimeSpan.FromHours(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await _movieService.SynchronizeWithRadarr(stoppingToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error occurred during Radarr synchronization: {Message}", e.Message);
        }
    }
}
