using LabProject.Application.DTOs;
using LabProject.Application.Notifications.Channels;
using LabProject.Domain.Entities;
using LabProject.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace LabProject.Infrastructure.Notifications.Channels;

public sealed class InAppChannel : INotificationChannel
{
    private readonly ILogger<InAppChannel> _logger;
    private readonly MongoNotificationRepository _mongoNotificationRepository;

    public InAppChannel(ILogger<InAppChannel> logger, MongoNotificationRepository mongoNotificationRepository)
    {
        _logger = logger;
        _mongoNotificationRepository = mongoNotificationRepository;
    }

    public ChannelType Type => ChannelType.InApp;

    public async Task<NotificationSendResult> SendAsync(ContractDTO ctx, CancellationToken ct)
    {
        _logger.LogInformation(
            "Mock InApp send: messageId {MessageId}, userId {UserId}",
            ctx.MessageId,
            ctx.UserId);

        var inAppNotification = new InAppNotification(
            MessageId: ctx.MessageId,
            UserId: ctx.UserId,
            Title: ctx.Metadata?["title"],
            Body: ctx.Metadata?["body"],
            CreatedAt: ctx.CreatedAt
        );

        var result = await _mongoNotificationRepository.SaveAsync<InAppNotification>(inAppNotification, ct);
        return new NotificationSendResult(Success: result, ErrorMessage: null);
    }
}