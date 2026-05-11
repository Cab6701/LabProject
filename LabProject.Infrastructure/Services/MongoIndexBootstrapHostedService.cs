using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LabProject.Infrastructure.Services;

public sealed class MongoIndexBootstrapHostedService : IHostedService
{
    private readonly MongoService _mongoService;
    private readonly ILogger<MongoIndexBootstrapHostedService> _logger;

    public MongoIndexBootstrapHostedService(
        MongoService mongoService,
        ILogger<MongoIndexBootstrapHostedService> logger)
    {
        _mongoService = mongoService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ensuring MongoDB indexes are created");
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _mongoService.CreateIndexes(cancellationToken);
                await _mongoService.CreateIndexesInApp(cancellationToken);
                await _mongoService.CreateIndexesProcessedNotification(cancellationToken);
                await _mongoService.CreateIndexesCampaign(cancellationToken);
                _logger.LogInformation("MongoDB indexes ensured successfully");
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == maxAttempts)
                {
                    _logger.LogError(ex, "Failed to ensure MongoDB indexes after {Attempts} attempts", maxAttempts);
                    return; // don't bring down the host for local dev
                }

                _logger.LogWarning(ex, "MongoDB not ready (attempt {Attempt}/{MaxAttempts}); retrying in {Delay}", attempt, maxAttempts, delay);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 15));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
