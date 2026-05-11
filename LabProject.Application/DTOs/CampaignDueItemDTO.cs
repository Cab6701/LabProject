namespace LabProject.Application.DTOs;

public readonly record struct CampaignDueItemDTO(
    string CampaignId,
    string Source,
    string[] UserIds,
    string[] Channels,
    Dictionary<string, string> Metadata,
    int RepeatType,
    DateTime NextRunAtUtc
);
