using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fin.Infrastructure.Notifications.Hubs;

[Authorize]
public class NotificationHub: Hub
{
}