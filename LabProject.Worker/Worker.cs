using Confluent.Kafka;

namespace LabProject.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = "localhost:9094",
            GroupId = "notification-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe("notification-topic");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(2000);
                if (consumeResult is null)
                    continue;

                _logger.LogInformation("Consumed message: {Message}", consumeResult.Message.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
            }
        }
    }
}