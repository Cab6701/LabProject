using LabProject.Application.DTOs;
using LabProject.Application.Interfaces;
using LabProject.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LabProject.Infrastructure.Repositories;

public sealed class MongoCampaignRepository : ICampaignRepository
{
    private const string CampaignCollection = "notification_campaigns";
    private readonly IMongoCollection<NotificationCampaignDocument> _collection;

    public MongoCampaignRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<NotificationCampaignDocument>(CampaignCollection);
    }

    public async Task<string> CreateAsync(NotificationCampaign campaign, CancellationToken cancellationToken = default)
    {
        var id = string.IsNullOrWhiteSpace(campaign.Id) ? ObjectId.GenerateNewId().ToString() : campaign.Id;
        var doc = NotificationCampaignDocument.FromDomain(campaign with { Id = id });
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return id;
    }

    public async Task<NotificationCampaign?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var doc = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        return doc?.ToDomain();
    }

    public async Task<bool> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default)
    {
        var update = Builders<NotificationCampaignDocument>.Update
            .Set(x => x.Enabled, enabled)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
        var result = await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task<IReadOnlyList<CampaignDueItemDTO>> GetDueCampaignsAsync(DateTime utcNow, int limit, CancellationToken cancellationToken = default)
    {
        var docs = await _collection
            .Find(x => x.Enabled && x.NextRunAtUtc <= utcNow)
            .SortBy(x => x.NextRunAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return docs.Select(x => new CampaignDueItemDTO(
            x.Id,
            x.Source,
            x.UserIds,
            x.Channels,
            x.Metadata,
            (int)x.RepeatType,
            x.NextRunAtUtc ?? x.ScheduleTimeUtc)).ToList();
    }

    public async Task UpdateNextRunAsync(string id, DateTime? nextRunAtUtc, CancellationToken cancellationToken = default)
    {
        var update = Builders<NotificationCampaignDocument>.Update
            .Set(x => x.NextRunAtUtc, nextRunAtUtc)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    private sealed class NotificationCampaignDocument
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string[] UserIds { get; init; } = [];
        public string[] Channels { get; init; } = [];
        public Dictionary<string, string> Metadata { get; init; } = [];
        public DateTime ScheduleTimeUtc { get; init; }
        public DateTime? NextRunAtUtc { get; init; }
        public RepeatType RepeatType { get; init; }
        public bool Enabled { get; init; }
        public string Source { get; init; } = "campaign";
        public DateTime CreatedAtUtc { get; init; }
        public DateTime UpdatedAtUtc { get; init; }

        public NotificationCampaign ToDomain() => new(
            Id, Name, UserIds, Channels, Metadata, ScheduleTimeUtc, NextRunAtUtc, RepeatType, Enabled, Source, CreatedAtUtc, UpdatedAtUtc);

        public static NotificationCampaignDocument FromDomain(NotificationCampaign campaign) => new()
        {
            Id = campaign.Id,
            Name = campaign.Name,
            UserIds = campaign.UserIds,
            Channels = campaign.Channels,
            Metadata = campaign.Metadata,
            ScheduleTimeUtc = campaign.ScheduleTimeUtc,
            NextRunAtUtc = campaign.NextRunAtUtc,
            RepeatType = campaign.RepeatType,
            Enabled = campaign.Enabled,
            Source = campaign.Source,
            CreatedAtUtc = campaign.CreatedAtUtc,
            UpdatedAtUtc = campaign.UpdatedAtUtc
        };
    }
}
