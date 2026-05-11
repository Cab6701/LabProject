using LabProject.Application.DTOs;

namespace LabProject.Application.Interfaces;

public interface INotificationPublisher
{
    Task SendNotificationAsync(ContractDTO message, CancellationToken cancellationToken = default);
    Task SendToRetryAsync(ContractDTO message, CancellationToken cancellationToken = default);
    Task SendToDlqAsync(ContractDTO message, CancellationToken cancellationToken = default);
}