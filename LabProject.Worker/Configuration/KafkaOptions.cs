namespace LabProject.Worker.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public string NotificationTopic { get; init; } = "notification-topic";
}
