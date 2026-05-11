using LabProject.Application.DTOs;
using LabProject.Domain.Entities;

namespace LabProject.Application.Interfaces;

public interface ICampaignRepository
{
    Task<string> CreateAsync(NotificationCampaign campaign, CancellationToken cancellationToken = default);
    Task<NotificationCampaign?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampaignDueItemDTO>> GetDueCampaignsAsync(DateTime utcNow, int limit, CancellationToken cancellationToken = default);
    Task UpdateNextRunAsync(string id, DateTime? nextRunAtUtc, CancellationToken cancellationToken = default);
}
