namespace LabProject.Api.Contracts;

public sealed record SendNotificationRequest(
    string? UserId,
    string? MessageId,
    string[]? Channels,
    int? Attempt,
    string? Source,
    DateTime? CreatedAt,
    Dictionary<string, string>? Metadata
);