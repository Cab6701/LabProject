using LabProject.Application.DTOs;
using LabProject.Application.Notifications.Channels;
using Microsoft.Extensions.Logging;

namespace LabProject.Infrastructure.Notifications.Channels;

public class EmailChannel : INotificationChannel
{
    private readonly ILogger<EmailChannel> _logger;
    public ChannelType Type => ChannelType.Email;

    public EmailChannel(ILogger<EmailChannel> logger)
    {
        _logger = logger;
    }

    public async Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        if (ctx.Metadata.TryGetValue("forceFail", out var forceFail) &&
            (string.Equals(forceFail, "all", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(forceFail, "email", StringComparison.OrdinalIgnoreCase)))
        {
            return new NotificationSendResult(false, "Forced email failure.");
        }

        _logger.LogInformation("Mock Email send: messageId {MessageId}, userId {UserId}", ctx.MessageId, ctx.UserId);
        return new NotificationSendResult(true, null);
    }
}