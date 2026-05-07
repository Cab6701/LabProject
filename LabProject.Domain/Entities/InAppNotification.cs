namespace LabProject.Domain.Entities;

public record InAppNotification(
    string MessageId,
    string UserId,
    string? Title,
    string? Body,
    DateTime CreatedAt
);