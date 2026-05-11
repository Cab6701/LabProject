using LabProject.Application.DTOs;
using LabProject.Application.Notifications.Channels;
using Microsoft.Extensions.Logging;

namespace LabProject.Infrastructure.Notifications.Channels;

public sealed class InAppChannel : INotificationChannel
{
    private readonly ILogger<InAppChannel> _logger;

    public InAppChannel(ILogger<InAppChannel> logger)
    {
        _logger = logger;
    }

    public ChannelType Type => ChannelType.InApp;

    public async Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        if (ctx.Metadata.TryGetValue("forceFail", out var forceFail) &&
            (string.Equals(forceFail, "all", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(forceFail, "inapp", StringComparison.OrdinalIgnoreCase)))
        {
            return new NotificationSendResult(false, "Forced inapp failure.");
        }

        _logger.LogInformation("Mock InApp send: messageId {MessageId}, userId {UserId}", ctx.MessageId, ctx.UserId);
        return new NotificationSendResult(true, null);
    }
}