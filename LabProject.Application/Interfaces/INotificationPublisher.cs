using LabProject.Application.DTOs;

namespace LabProject.Application.Interfaces;

public interface INotificationPublisher
{
    Task SendNotificationAsync(ContractDTO message, CancellationToken cancellationToken = default);
}