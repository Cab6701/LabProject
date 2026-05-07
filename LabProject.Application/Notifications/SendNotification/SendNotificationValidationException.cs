namespace LabProject.Application.Notifications.SendNotification;

public sealed class SendNotificationValidationException : Exception
{
    public SendNotificationValidationException(Dictionary<string, string[]> errors)
        : base("Send notification request is invalid.")
    {
        Errors = errors;
    }

    public Dictionary<string, string[]> Errors { get; }
}
