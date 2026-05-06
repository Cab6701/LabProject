namespace LabProject.Application.DTOs;

public record struct ContractDTO(
    string UserId,
    string MessageId,
    string[] Channels,
    int Attempt,
    string Source,
    DateTime CreatedAt,
    Dictionary<string, string> Metadata
);