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

    public async Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        if (ctx.Metadata.TryGetValue("forceFail", out var forceFail) &&
            (string.Equals(forceFail, "all", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(forceFail, "firebase", StringComparison.OrdinalIgnoreCase)))
        {
            return new NotificationSendResult(false, "Forced firebase failure.");
        }

        _logger.LogInformation("Mock Firebase send: messageId {MessageId}, userId {UserId}", ctx.MessageId, ctx.UserId);
        return new NotificationSendResult(true, null);
    }
}