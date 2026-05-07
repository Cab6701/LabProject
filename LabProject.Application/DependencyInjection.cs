using LabProject.Application.Notifications.SendNotification;
using Microsoft.Extensions.DependencyInjection;

namespace LabProject.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISendNotificationUseCase, SendNotificationUseCase>();
        return services;
    }
}
