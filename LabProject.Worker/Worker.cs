using System.Text.Json;
using Confluent.Kafka;
using LabProject.Application.DTOs;
using LabProject.Application.Notifications.Channels;
using LabProject.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace LabProject.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IReadOnlyDictionary<ChannelType, INotificationChannel> _channelsByType;

    public Worker(
        ILogger<Worker> logger,
        IOptions<KafkaOptions> kafkaOptions,
        IEnumerable<INotificationChannel> channels)
    {
        _logger = logger;
        _kafkaOptions = kafkaOptions.Value;
        _channelsByType = channels.ToDictionary(c => c.Type);
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

                foreach (var c in notification.Channels)
                {
                    var channelType = ParseChannelType(c);
                    if (!_channelsByType.TryGetValue(channelType, out var channel))
                    {
                        _logger.LogWarning(
                            "No channel strategy registered for type {ChannelType}. Skipped. messageId {MessageId}, userId {UserId}",
                            channelType,
                            notification.MessageId,
                            notification.UserId);
                        continue;
                    }

                    var result = await channel.SendAsync(notification, stoppingToken);
                    if (!result.Success)
                    {
                        _logger.LogWarning(
                            "Channel {ChannelType} send failed. messageId {MessageId}, userId {UserId}, error {ErrorMessage}",
                            channelType,
                            notification.MessageId,
                            notification.UserId,
                            result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
            }
        }
    }

    private static ChannelType ParseChannelType(string channel)
    {
        return channel.Trim().ToLowerInvariant() switch
        {
            "email" => ChannelType.Email,
            "firebase" => ChannelType.Firebase,
            "inapp" => ChannelType.InApp,
            "in_app" => ChannelType.InApp,
            _ => throw new NotSupportedException($"Unknown channel '{channel}'")
        };
    }
}