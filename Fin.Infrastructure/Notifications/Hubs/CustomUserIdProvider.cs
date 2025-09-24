using Microsoft.AspNetCore.SignalR;

namespace Fin.Infrastructure.Notifications.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return connection.User.FindFirst("userId")?.Value;
    }
}