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

    public Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct)
    {
        _logger.LogInformation(
            "Mock Email send: messageId {MessageId}, userId {UserId}",
            ctx.MessageId,
            ctx.UserId);
        return Task.FromResult(new NotificationSendResult(Success: true, ErrorMessage: null));
    }
}