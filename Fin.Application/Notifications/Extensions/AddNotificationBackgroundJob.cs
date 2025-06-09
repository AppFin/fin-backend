using Fin.Application.Notifications.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Application.Notifications.Extensions;

public static class AddNotificationBackgroundJob
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        services.AddSignalR();
        return services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
    }
}