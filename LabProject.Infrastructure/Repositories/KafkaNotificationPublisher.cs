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
        await SendToTopicAsync(_kafkaOptions.NotificationTopic, message, cancellationToken);
    }

    public async Task SendToRetryAsync(ContractDTO message, CancellationToken cancellationToken = default)
    {
        await SendToTopicAsync(_kafkaOptions.RetryTopic, message, cancellationToken);
    }

    public async Task SendToDlqAsync(ContractDTO message, CancellationToken cancellationToken = default)
    {
        await SendToTopicAsync(_kafkaOptions.DlqTopic, message, cancellationToken);
    }

    private async Task SendToTopicAsync(string topic, ContractDTO message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = message.UserId,
            Value = payload
        };

        var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
        _logger.LogInformation(
            "Published messageId {MessageId} to topic {Topic} at offset {Offset}",
            message.MessageId,
            result.Topic,
            result.Offset.Value);
    }
}