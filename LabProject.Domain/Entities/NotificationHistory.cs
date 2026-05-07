namespace LabProject.Domain.Entities;

public record NotificationHistory(
    string MessageId,
    string UserId,
    string Channel,
    Status Status,
    string? ErrorMessage,
    DateTime SentAt,
    string? DriverCode
);

public enum Status
{
    Success,
    Failed
}