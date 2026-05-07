using System.Text.Json;
using Confluent.Kafka;
using LabProject.Application.DTOs;
using LabProject.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace LabProject.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly KafkaOptions _kafkaOptions;

    public Worker(ILogger<Worker> logger, IOptions<KafkaOptions> kafkaOptions)
    {
        _logger = logger;
        _kafkaOptions = kafkaOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = "notification-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_kafkaOptions.NotificationTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(2000);
                if (consumeResult is null)
                    continue;

                var message = JsonSerializer.Deserialize<ContractDTO?>(consumeResult.Message.Value);
                if (!message.HasValue)
                {
                    _logger.LogWarning("Skipped malformed message at offset {Offset}", consumeResult.Offset.Value);
                    continue;
                }
                var notification = message.Value;

                _logger.LogInformation(
                    "Consumed messageId {MessageId} for userId {UserId} channels {Channels} attempt {Attempt} source {Source}",
                    notification.MessageId,
                    notification.UserId,
                    string.Join(",", notification.Channels),
                    notification.Attempt,
                    notification.Source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
            }
        }
    }
}