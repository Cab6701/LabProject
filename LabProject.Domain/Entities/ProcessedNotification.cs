namespace LabProject.Domain.Entities;

public record ProcessedNotification(
    string MessageId,
    string UserId,
    string Channel,
    DateTime ProcessedAt
);