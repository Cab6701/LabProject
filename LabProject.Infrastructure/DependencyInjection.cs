using Confluent.Kafka;
using LabProject.Application.Interfaces;
using LabProject.Application.Notifications.Channels;
using LabProject.Infrastructure.Configuration;
using LabProject.Infrastructure.Notifications.Channels;
using LabProject.Infrastructure.Repositories;
using LabProject.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

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

        var mongoOptions = configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>() ?? new MongoOptions();
        services.AddSingleton(mongoOptions);
        var clickHouseOptions = configuration.GetSection(ClickHouseOptions.SectionName).Get<ClickHouseOptions>() ?? new ClickHouseOptions();
        services.AddSingleton(clickHouseOptions);

        services.AddSingleton<IMongoClient>(new MongoClient(mongoOptions.ConnectionString));

        services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoOptions.Database));

        services.AddScoped<INotificationPublisher, KafkaNotificationPublisher>();

        services.AddSingleton<MongoNotificationRepository>();
        services.AddSingleton<ICampaignRepository, MongoCampaignRepository>();

        services.AddSingleton<MongoService>();
        services.AddHttpClient<ClickHouseService>();
        services.AddSingleton<IAnalyticsStore>(sp => sp.GetRequiredService<ClickHouseService>());
        services.AddHostedService<MongoIndexBootstrapHostedService>();
        services.AddHostedService<ClickHouseBootstrapHostedService>();
        services.AddSingleton<INotificationChannel, InAppChannel>();
        services.AddSingleton<INotificationChannel, FirebaseChannel>();
        services.AddSingleton<INotificationChannel, EmailChannel>();

        return services;
    }
}
