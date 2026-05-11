using System.Net.Http.Headers;
using System.Text;
using LabProject.Application.Interfaces;
using LabProject.Domain.Entities;
using LabProject.Infrastructure.Configuration;

namespace LabProject.Infrastructure.Services;

public sealed class ClickHouseService : IAnalyticsStore
{
    private readonly HttpClient _httpClient;

    public ClickHouseService(HttpClient httpClient, ClickHouseOptions options)
    {
        _httpClient = httpClient;
        var endpoint = ParseHttpEndpoint(options.ConnectionString);
        _httpClient.BaseAddress = new Uri(endpoint);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
    }

    public async Task WriteEventAsync(NotificationHistory history, CancellationToken cancellationToken = default)
    {
        var error = history.ErrorMessage?.Replace("'", "''") ?? string.Empty;
        var driverCode = history.DriverCode?.Replace("'", "''") ?? string.Empty;
        var sql =
            $"INSERT INTO notification_events FORMAT Values ('{history.MessageId}','{history.UserId}','{history.Channel}','{history.Status}'," +
            $"'{error}','{driverCode}',toDateTime('{history.SentAt:yyyy-MM-dd HH:mm:ss}'))";
        await ExecuteNonQueryAsync(sql, cancellationToken);
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
CREATE TABLE IF NOT EXISTS notification_events
(
    messageId String,
    userId String,
    channel String,
    status String,
    errorMessage String,
    driverCode String,
    sentAt DateTime
)
ENGINE = MergeTree
ORDER BY (sentAt, messageId, userId, channel)
""";
        await ExecuteNonQueryAsync(sql, cancellationToken);
    }

    public async Task<int> GetTotalAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT count() FROM notification_events {BuildWhereClause(fromUtc, toUtc)}";
        var value = await ExecuteScalarAsync(sql, cancellationToken);
        return int.TryParse(value, out var result) ? result : 0;
    }

    public async Task<IDictionary<string, int>> GetByChannelAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT channel, count() FROM notification_events {BuildWhereClause(fromUtc, toUtc)} GROUP BY channel";
        return await ExecuteMapAsync(sql, cancellationToken);
    }

    public async Task<double> GetSuccessRateAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT if(count()=0,0,sum(status='Success')*100.0/count()) FROM notification_events {BuildWhereClause(fromUtc, toUtc)}";
        var value = await ExecuteScalarAsync(sql, cancellationToken);
        return double.TryParse(value, out var result) ? result : 0;
    }

    public async Task<IDictionary<string, int>> GetByDriverCodeAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT driverCode, count() FROM notification_events {BuildWhereClause(fromUtc, toUtc)} GROUP BY driverCode";
        return await ExecuteMapAsync(sql, cancellationToken);
    }

    private static string ParseHttpEndpoint(string connectionString)
    {
        var host = "localhost";
        var port = "8123";
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (keyValue.Length != 2) continue;
            if (keyValue[0].Equals("Host", StringComparison.OrdinalIgnoreCase)) host = keyValue[1];
            if (keyValue[0].Equals("Port", StringComparison.OrdinalIgnoreCase)) port = keyValue[1];
        }

        return $"http://{host}:{port}/";
    }

    private static string BuildWhereClause(DateTime? fromUtc, DateTime? toUtc)
    {
        var clauses = new List<string>();
        if (fromUtc.HasValue) clauses.Add($"sentAt >= toDateTime('{fromUtc.Value:yyyy-MM-dd HH:mm:ss}')");
        if (toUtc.HasValue) clauses.Add($"sentAt <= toDateTime('{toUtc.Value:yyyy-MM-dd HH:mm:ss}')");
        return clauses.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", clauses)}";
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        using var content = new StringContent(sql, Encoding.UTF8, "text/plain");
        using var response = await _httpClient.PostAsync(string.Empty, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> ExecuteScalarAsync(string sql, CancellationToken cancellationToken)
    {
        using var content = new StringContent(sql, Encoding.UTF8, "text/plain");
        using var response = await _httpClient.PostAsync(string.Empty, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
    }

    private async Task<IDictionary<string, int>> ExecuteMapAsync(string sql, CancellationToken cancellationToken)
    {
        using var content = new StringContent(sql, Encoding.UTF8, "text/plain");
        using var response = await _httpClient.PostAsync(string.Empty, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var lines = (await response.Content.ReadAsStringAsync(cancellationToken))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var parts = line.Split('\t', 2);
            if (parts.Length != 2) continue;
            if (int.TryParse(parts[1], out var count))
            {
                result[parts[0]] = count;
            }
        }

        return result;
    }
}
