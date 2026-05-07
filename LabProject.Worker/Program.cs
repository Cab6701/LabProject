using LabProject.Worker;
using LabProject.Worker.Configuration;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((context, services) =>
{
    services.Configure<KafkaOptions>(context.Configuration.GetSection(KafkaOptions.SectionName));
    services.AddHostedService<Worker>();
});

var host = builder.Build();

host.Run();
