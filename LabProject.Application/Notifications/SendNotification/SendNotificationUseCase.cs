using LabProject.Application.DTOs;
using LabProject.Application.Interfaces;

namespace LabProject.Application.Notifications.SendNotification;

public sealed class SendNotificationUseCase : ISendNotificationUseCase
{
    private static readonly HashSet<string> AllowedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "firebase",
        "inapp"
    };

    private readonly INotificationPublisher _publisher;

    public SendNotificationUseCase(INotificationPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<string> ExecuteAsync(SendNotificationCommand command, CancellationToken cancellationToken = default)
    {
        var errors = Validate(command);
        if (errors.Count > 0)
        {
            throw new SendNotificationValidationException(errors);
        }

        var dto = new ContractDTO(
            UserId: command.UserId!,
            MessageId: string.IsNullOrWhiteSpace(command.MessageId) ? Guid.NewGuid().ToString("N") : command.MessageId!,
            Channels: command.Channels!,
            Attempt: command.Attempt ?? 0,
            Source: command.Source!,
            CreatedAt: command.CreatedAt ?? DateTime.UtcNow,
            Metadata: command.Metadata ?? new Dictionary<string, string>()
        );

        await _publisher.SendNotificationAsync(dto, cancellationToken);
        return dto.MessageId;
    }

    private static Dictionary<string, string[]> Validate(SendNotificationCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            errors["userId"] = ["userId is required."];
        }

        if (string.IsNullOrWhiteSpace(command.Source))
        {
            errors["source"] = ["source is required."];
        }

        if (command.Channels is null || command.Channels.Length == 0)
        {
            errors["channels"] = ["channels is required and must contain at least 1 item."];
        }
        else if (command.Channels.Any(c => string.IsNullOrWhiteSpace(c) || !AllowedChannels.Contains(c)))
        {
            errors["channels"] = ["channels contains invalid value. Allowed: email, firebase, inapp."];
        }

        if (command.Attempt is < 0)
        {
            errors["attempt"] = ["attempt must be >= 0."];
        }

        if (command.Metadata is null)
        {
            errors["metadata"] = ["metadata is required (can be empty object)."];
        }

        return errors;
    }
}
