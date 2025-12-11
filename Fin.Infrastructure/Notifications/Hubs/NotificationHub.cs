using Fin.Infrastructure.AmbientDatas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Notifications.Hubs;

[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger, IAmbientData ambientData): Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        var tenantIdClaim = Context.User?.FindFirst("tenantId")?.Value;
        var displayNameClaim = Context.User?.FindFirst("displayName")?.Value;
        var isAdminClaim = Context.User?.FindFirst("isAdmin")?.Value;
        
        if (Guid.TryParse(userIdClaim, out var userId) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            var isAdmin = bool.TryParse(isAdminClaim, out var admin) && admin;
            ambientData.SetData(tenantId, userId, displayNameClaim ?? "", isAdmin);
        }
        
        logger.LogInformation(
            "User {UserId} connected to WebSocket. ConnectionId: {ConnectionId}",
            userIdClaim,
            Context.ConnectionId
        );

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = ambientData.UserId.GetValueOrDefault();
        
        logger.LogInformation(
            "User {UserId} disconnected from WebSocket. ConnectionId: {ConnectionId}",
            userId,
            Context.ConnectionId
        );

        await base.OnDisconnectedAsync(exception);
    }
}