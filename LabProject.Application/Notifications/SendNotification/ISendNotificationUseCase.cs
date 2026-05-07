namespace LabProject.Application.Notifications.SendNotification;

public interface ISendNotificationUseCase
{
    Task<string> ExecuteAsync(SendNotificationCommand command, CancellationToken cancellationToken = default);
}
