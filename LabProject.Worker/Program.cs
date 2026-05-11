using LabProject.Worker;
using LabProject.Infrastructure;
using LabProject.Infrastructure.Configuration;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((context, services) =>
{
    services.Configure<KafkaOptions>(context.Configuration.GetSection(KafkaOptions.SectionName));
    services.AddInfrastructure(context.Configuration);
    services.AddHostedService<Worker>();
});

var host = builder.Build();

host.Run();
