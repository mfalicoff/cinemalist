using System;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaList.Api.HostedServices;

public class ScraperInitializerService(
    IServiceProvider serviceProvider,
    ILogger<ScraperInitializerService> logger
) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<ScraperInitializerService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunScrapers(stoppingToken);

        using PeriodicTimer timer = new(TimeSpan.FromHours(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunScrapers(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scraper service is stopping.");
        }
    }

    private async Task RunScrapers(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting scraper run at {Time}", DateTime.UtcNow);

            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
            IScraperService scraperService =
                scope.ServiceProvider.GetRequiredService<IScraperService>();
            await scraperService.ScrapeFilmsAsync(cancellationToken);

            _logger.LogInformation("Completed scraper run at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in RunScrapers");
        }
    }
}
