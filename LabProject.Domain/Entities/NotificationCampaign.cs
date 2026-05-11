namespace LabProject.Domain.Entities;

public record NotificationCampaign(
    string Id,
    string Name,
    string[] UserIds,
    string[] Channels,
    Dictionary<string, string> Metadata,
    DateTime ScheduleTimeUtc,
    DateTime? NextRunAtUtc,
    RepeatType RepeatType,
    bool Enabled,
    string Source,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
