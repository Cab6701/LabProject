namespace LabProject.Api.Contracts;

public sealed record CreateCampaignRequest(
    string? Name,
    string[]? UserIds,
    string[]? Channels,
    Dictionary<string, string>? Metadata,
    DateTime? ScheduleTimeUtc,
    int? RepeatType,
    bool? Enabled,
    string? Source
);
