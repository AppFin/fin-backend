using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Notifications.Hubs;

public static class AddNotificationBackgroundJob
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        services.AddSignalR();
        return services
            .AddSingleton<IUserIdProvider, CustomUserIdProvider>()
            .AddSingleton<IWebSocketTokenService, WebSocketTokenService>();

    }
}