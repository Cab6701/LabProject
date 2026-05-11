namespace LabProject.Infrastructure.Configuration;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public string NotificationTopic { get; init; } = "notification-topic";
    public string RetryTopic { get; init; } = "notification-retry";
    public string DlqTopic { get; init; } = "notification-dlq";
}
