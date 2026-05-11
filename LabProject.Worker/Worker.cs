using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using LabProject.Application.DTOs;
using LabProject.Application.Interfaces;
using LabProject.Application.Notifications.Channels;
using LabProject.Domain.Entities;
using LabProject.Infrastructure.Repositories;
using LabProject.Infrastructure.Services;
using LabProject.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace LabProject.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IReadOnlyDictionary<ChannelType, INotificationChannel> _channelsByType;
    private readonly MongoNotificationRepository _notificationRepository;
    private readonly MongoService _mongoService;
    private readonly INotificationPublisher _publisher;
    private readonly IAnalyticsStore _analyticsStore;

    public Worker(
        ILogger<Worker> logger,
        IOptions<KafkaOptions> kafkaOptions,
        IEnumerable<INotificationChannel> channels,
        MongoNotificationRepository notificationRepository,
        MongoService mongoService,
        INotificationPublisher publisher,
        IAnalyticsStore analyticsStore)
    {
        _logger = logger;
        _kafkaOptions = kafkaOptions.Value;
        _channelsByType = channels.ToDictionary(c => c.Type);
        _notificationRepository = notificationRepository;
        _mongoService = mongoService;
        _publisher = publisher;
        _analyticsStore = analyticsStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = "notification-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        await EnsureTopicsAsync(stoppingToken);

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe([_kafkaOptions.NotificationTopic, _kafkaOptions.RetryTopic]);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult is null)
                    {
                        continue;
                    }

                    var message = JsonSerializer.Deserialize<ContractDTO?>(consumeResult.Message.Value);
                    if (!message.HasValue)
                    {
                        _logger.LogWarning("Skipped malformed message at offset {Offset}", consumeResult.Offset.Value);
                        continue;
                    }

                    var notification = message.Value;
                    var tasks = notification.Channels.Select(channelName => ProcessChannelAsync(notification, channelName, stoppingToken));
                    await Task.WhenAll(tasks);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {ErrorReason}", ex.Error.Reason);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing message");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer is stopping.");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task EnsureTopicsAsync(CancellationToken cancellationToken)
    {
        var adminConfig = new AdminClientConfig { BootstrapServers = _kafkaOptions.BootstrapServers };
        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        var topics = new[]
        {
            new TopicSpecification { Name = _kafkaOptions.NotificationTopic, NumPartitions = 3, ReplicationFactor = 1 },
            new TopicSpecification { Name = _kafkaOptions.RetryTopic, NumPartitions = 3, ReplicationFactor = 1 },
            new TopicSpecification { Name = _kafkaOptions.DlqTopic, NumPartitions = 3, ReplicationFactor = 1 }
        };

        try
        {
            await adminClient.CreateTopicsAsync(topics, new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });
            _logger.LogInformation("Ensured Kafka topics: {Topics}", string.Join(", ", topics.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex)
        {
            // "Topic already exists" is expected when services restart.
            var unexpected = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (unexpected.Count > 0)
            {
                foreach (var item in unexpected)
                {
                    _logger.LogWarning(
                        "Create topic warning for {Topic}: {Reason}",
                        item.Topic,
                        item.Error.Reason);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not ensure Kafka topics before consuming.");
        }
    }

    private async Task ProcessChannelAsync(ContractDTO notification, string channelName, CancellationToken stoppingToken)
    {
        if (!TryParseChannelType(channelName, out var channelType))
        {
            _logger.LogWarning("Unknown channel '{Channel}' skipped. messageId {MessageId}", channelName, notification.MessageId);
            return;
        }

        if (!_channelsByType.TryGetValue(channelType, out var channel))
        {
            _logger.LogWarning("No channel strategy for type {ChannelType}. messageId {MessageId}", channelType, notification.MessageId);
            return;
        }

        var processed = new ProcessedNotification(
            MessageId: notification.MessageId,
            UserId: notification.UserId,
            Channel: channelType.ToString().ToLowerInvariant(),
            ProcessedAt: DateTime.UtcNow);
        var shouldProcess = await _mongoService.SaveProcessedNotificationAsync(processed, stoppingToken);
        if (!shouldProcess)
        {
            return;
        }

        var result = await channel.SendAsync(notification, stoppingToken);
        await PersistOutcomeAsync(notification, channelType, result, stoppingToken);
        if (!result.Success)
        {
            await HandleRetryOrDlqAsync(notification, stoppingToken);
        }
    }

    private async Task PersistOutcomeAsync(
        ContractDTO notification,
        ChannelType channelType,
        NotificationSendResult result,
        CancellationToken cancellationToken)
    {
        var history = new NotificationHistory(
            MessageId: notification.MessageId,
            UserId: notification.UserId,
            Channel: channelType.ToString().ToLowerInvariant(),
            Status: result.Success ? Status.Success : Status.Failed,
            ErrorMessage: result.ErrorMessage,
            SentAt: DateTime.UtcNow,
            DriverCode: notification.Metadata.TryGetValue("driverCode", out var code) ? code : null);
        await _notificationRepository.SaveAsync(history, cancellationToken);
        if (result.Success && channelType == ChannelType.InApp)
        {
            var inApp = new InAppNotification(
                MessageId: notification.MessageId,
                UserId: notification.UserId,
                Title: notification.Metadata.TryGetValue("title", out var title) ? title : null,
                Body: notification.Metadata.TryGetValue("body", out var body) ? body : null,
                CreatedAt: notification.CreatedAt);
            await _notificationRepository.SaveAsync(inApp, cancellationToken);
        }
        await _analyticsStore.WriteEventAsync(history, cancellationToken);
    }

    private async Task HandleRetryOrDlqAsync(ContractDTO notification, CancellationToken cancellationToken)
    {
        var retryMessage = notification with { Attempt = notification.Attempt + 1 };
        if (retryMessage.Attempt <= 1)
        {
            await _publisher.SendToRetryAsync(retryMessage, cancellationToken);
            return;
        }

        await _publisher.SendToDlqAsync(retryMessage, cancellationToken);
    }

    private static bool TryParseChannelType(string channel, out ChannelType type)
    {
        switch (channel.Trim().ToLowerInvariant())
        {
            case "email":
                type = ChannelType.Email;
                return true;
            case "firebase":
                type = ChannelType.Firebase;
                return true;
            case "inapp":
            case "in_app":
                type = ChannelType.InApp;
                return true;
            default:
                type = default;
                return false;
        }
    }
}