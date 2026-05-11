using LabProject.Domain.Entities;

namespace LabProject.Application.Interfaces;

public interface IAnalyticsStore
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);
    Task WriteEventAsync(NotificationHistory history, CancellationToken cancellationToken = default);
    Task<int> GetTotalAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<IDictionary<string, int>> GetByChannelAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<double> GetSuccessRateAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<IDictionary<string, int>> GetByDriverCodeAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
