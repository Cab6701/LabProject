namespace LabProject.Domain.Entities;

public record Contract(
    string MessageId,
    string UserId,
    string[] Channels,
    int Attempt,
    string Source,
    DateTime CreatedAt,
    Dictionary<string, string> Metadata
);