using LabProject.Application.DTOs;
using LabProject.Domain.Entities;

namespace LabProject.Application.Notifications.Channels;

public interface INotificationChannel
{
    ChannelType Type { get; }
    Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct);
}