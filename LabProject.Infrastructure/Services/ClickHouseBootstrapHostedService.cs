using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LabProject.Infrastructure.Services;

public sealed class ClickHouseBootstrapHostedService : IHostedService
{
    private readonly ClickHouseService _clickHouseService;
    private readonly ILogger<ClickHouseBootstrapHostedService> _logger;

    public ClickHouseBootstrapHostedService(
        ClickHouseService clickHouseService,
        ILogger<ClickHouseBootstrapHostedService> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _clickHouseService.EnsureSchemaAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouse health check failed during startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
