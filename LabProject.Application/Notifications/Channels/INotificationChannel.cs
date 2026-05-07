using LabProject.Application.DTOs;

namespace LabProject.Application.Notifications.Channels;

public interface INotificationChannel
{
    ChannelType Type { get; }
    Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct);
}