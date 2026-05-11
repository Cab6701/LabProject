namespace LabProject.Api.Contracts;

public sealed record SendNotificationBulkRequest(
    string[]? UserIds,
    string[]? Channels,
    string? Source,
    Dictionary<string, string>? Metadata
);
