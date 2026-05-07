using LabProject.Domain.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LabProject.Infrastructure.Repositories;

public sealed class MongoNotificationRepository
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoNotificationRepository> _logger;
    public MongoNotificationRepository(IMongoDatabase database, ILogger<MongoNotificationRepository> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<bool> SaveAsync<T>(T collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.GetCollection<T>(GetCollectionName(collectionName)).InsertOneAsync(collectionName, cancellationToken: cancellationToken);
            _logger.LogInformation("Data saved successfully: {CollectionName}", collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save data: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    private string GetCollectionName<T>(T entity)
    {
        if (entity is InAppNotification)
        {
            return "in_app_notifications";
        }

        return "notification_history";
    }
}