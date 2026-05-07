namespace LabProject.Application.Notifications.SendNotification;

public sealed record SendNotificationCommand(
    string? UserId,
    string? MessageId,
    string[]? Channels,
    int? Attempt,
    string? Source,
    DateTime? CreatedAt,
    Dictionary<string, string>? Metadata
);
