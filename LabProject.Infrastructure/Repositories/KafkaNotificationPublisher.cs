using LabProject.Application.DTOs;
using LabProject.Application.Interfaces;
using LabProject.Infrastructure.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LabProject.Infrastructure.Repositories;

public class KafkaNotificationPublisher : INotificationPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<KafkaNotificationPublisher> _logger;

    public KafkaNotificationPublisher(
        IProducer<string, string> producer,
        KafkaOptions kafkaOptions,
        ILogger<KafkaNotificationPublisher> logger)
    {
        _producer = producer;
        _kafkaOptions = kafkaOptions;
        _logger = logger;
    }

    public async Task SendNotificationAsync(ContractDTO message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = message.UserId,
            Value = payload
        };

        var result = await _producer.ProduceAsync(_kafkaOptions.NotificationTopic, kafkaMessage, cancellationToken);
        _logger.LogInformation(
            "Published messageId {MessageId} to topic {Topic} at offset {Offset}",
            message.MessageId,
            result.Topic,
            result.Offset.Value);
    }
}