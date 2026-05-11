namespace LabProject.Infrastructure.Configuration;

public sealed class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";
    public string ConnectionString { get; init; } = string.Empty;
}
