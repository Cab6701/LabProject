using LabProject.Domain.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LabProject.Infrastructure.Services;

public class MongoService
{
    private const string NotificationHistoryCollection = "notification_history";
    private const string InAppNotificationCollection = "in_app_notifications";
    private const string ProcessedNotificationCollection = "processed_notifications";
    private const string CampaignCollection = "notification_campaigns";
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoService> _logger;
    public MongoService(IMongoDatabase database, ILogger<MongoService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task CreateIndexes(CancellationToken cancellationToken = default)
    {
        var indexModel = new CreateIndexModel<NotificationHistory>(Builders<NotificationHistory>.IndexKeys
                .Ascending(h => h.MessageId)
                .Ascending(h => h.UserId)
                .Ascending(h => h.Channel));
        await _database.GetCollection<NotificationHistory>(NotificationHistoryCollection)
            .Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }

    public async Task CreateIndexesInApp(CancellationToken cancellationToken = default)
    {
        var indexModel = new CreateIndexModel<InAppNotification>(Builders<InAppNotification>.IndexKeys
                .Ascending(h => h.MessageId)
                .Ascending(h => h.UserId));
        await _database.GetCollection<InAppNotification>(InAppNotificationCollection)
            .Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }

    public async Task CreateIndexesProcessedNotification(CancellationToken cancellationToken = default)
    {
        var keys = Builders<ProcessedNotification>.IndexKeys
            .Ascending(h => h.MessageId)
            .Ascending(h => h.UserId)
            .Ascending(h => h.Channel);
        var indexModel = new CreateIndexModel<ProcessedNotification>(keys, new CreateIndexOptions { Unique = true });
        await _database.GetCollection<ProcessedNotification>(ProcessedNotificationCollection)
            .Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }

    public async Task CreateIndexesCampaign(CancellationToken cancellationToken = default)
    {
        var keys = Builders<NotificationCampaign>.IndexKeys
            .Ascending(c => c.Enabled)
            .Ascending(c => c.NextRunAtUtc);
        var indexModel = new CreateIndexModel<NotificationCampaign>(keys);
        await _database.GetCollection<NotificationCampaign>(CampaignCollection)
            .Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }

    public async Task<bool> SaveProcessedNotificationAsync(ProcessedNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.GetCollection<ProcessedNotification>(ProcessedNotificationCollection).InsertOneAsync(notification, cancellationToken: cancellationToken);
            _logger.LogInformation("Saved processed notification: {MessageId} for user {UserId} on channel {Channel}", notification.MessageId, notification.UserId, notification.Channel);
            return true;
        }
        catch (Exception ex)
        {
            if (ex is MongoDuplicateKeyException)
            {
                _logger.LogWarning("Duplicate processed notification: {MessageId} for user {UserId} on channel {Channel}", notification.MessageId, notification.UserId, notification.Channel);
            }
            else
            {
                _logger.LogError(ex, "Error saving processed notification: {MessageId} for user {UserId} on channel {Channel}", notification.MessageId, notification.UserId, notification.Channel);
            }

            return false;
        }
    }
}