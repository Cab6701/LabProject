using LabProject.Application.DTOs;
using LabProject.Application.Notifications.Channels;
using Microsoft.Extensions.Logging;

namespace LabProject.Infrastructure.Notifications.Channels;

public class FirebaseChannel : INotificationChannel
{
    private readonly ILogger<FirebaseChannel> _logger;
    public ChannelType Type => ChannelType.Firebase;

    public FirebaseChannel(ILogger<FirebaseChannel> logger)
    {
        _logger = logger;
    }

    public Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct)
    {
         _logger.LogInformation(
            "Mock Firebase send: messageId {MessageId}, userId {UserId}",
            ctx.MessageId,
            ctx.UserId);
        return Task.FromResult(new NotificationSendResult(Success: true, ErrorMessage: null));
    }
}