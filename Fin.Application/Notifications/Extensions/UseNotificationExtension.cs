using Fin.Infrastructure.Notifications.Hubs;
using Microsoft.AspNetCore.Builder;

namespace Fin.Application.Notifications.Extensions;

public static class UseNotificationExtension
{
    public static void UseNotifications(this WebApplication app)
    {
        app.MapHub<NotificationHub>("/notifications");
    }
    
}