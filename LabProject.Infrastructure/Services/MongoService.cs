using LabProject.Domain.Entities;
using MongoDB.Driver;

namespace LabProject.Infrastructure.Services;

public class MongoService
{
    private const string NotificationHistoryCollection = "notification_history";
    private const string InAppNotificationCollection = "in_app_notifications";
    private readonly IMongoDatabase _database;
    public MongoService(IMongoDatabase database)
    {
        _database = database;
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
}