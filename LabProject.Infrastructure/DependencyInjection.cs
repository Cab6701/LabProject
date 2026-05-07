using Confluent.Kafka;
using LabProject.Application.Interfaces;
using LabProject.Infrastructure.Configuration;
using LabProject.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabProject.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var kafkaOptions = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
        services.AddSingleton(kafkaOptions);

        services.AddSingleton<IProducer<string, string>>(_ =>
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers
            };

            return new ProducerBuilder<string, string>(producerConfig).Build();
        });

        services.AddScoped<INotificationPublisher, KafkaNotificationPublisher>();

        return services;
    }
}
