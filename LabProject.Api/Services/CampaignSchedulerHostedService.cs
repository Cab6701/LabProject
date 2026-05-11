using LabProject.Application.DTOs;
using LabProject.Application.Interfaces;
using LabProject.Domain.Entities;

namespace LabProject.Api.Services;

public sealed class CampaignSchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CampaignSchedulerHostedService> _logger;

    public CampaignSchedulerHostedService(IServiceProvider serviceProvider, ILogger<CampaignSchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var campaignRepository = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();
                var dueItems = await campaignRepository.GetDueCampaignsAsync(DateTime.UtcNow, 50, stoppingToken);
                foreach (var due in dueItems)
                {
                    foreach (var userId in due.UserIds)
                    {
                        var dto = new ContractDTO(
                            UserId: userId,
                            MessageId: Guid.NewGuid().ToString("N"),
                            Channels: due.Channels,
                            Attempt: 0,
                            Source: due.Source,
                            CreatedAt: DateTime.UtcNow,
                            Metadata: due.Metadata);
                        await publisher.SendNotificationAsync(dto, stoppingToken);
                    }

                    var nextRun = ComputeNextRun(due.NextRunAtUtc, (RepeatType)due.RepeatType);
                    await campaignRepository.UpdateNextRunAsync(due.CampaignId, nextRun, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Campaign scheduler failed in current cycle.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private static DateTime? ComputeNextRun(DateTime baseTimeUtc, RepeatType repeatType) =>
        repeatType switch
        {
            RepeatType.None => null,
            RepeatType.Daily => baseTimeUtc.AddDays(1),
            RepeatType.Weekly => baseTimeUtc.AddDays(7),
            RepeatType.Monthly => baseTimeUtc.AddMonths(1),
            _ => null
        };
}
