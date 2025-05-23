using Microsoft.AspNetCore.SignalR;

namespace Fin.Application.Notifications.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("user_id")?.Value;
    }
}