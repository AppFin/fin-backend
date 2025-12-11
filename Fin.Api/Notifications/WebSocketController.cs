using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Notifications.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Notifications;

[Route("notifications/websocket")]
[Authorize] 
public class WebSocketController(IWebSocketTokenService tokenService, IAmbientData ambientData): ControllerBase
{
    [HttpPost("connection-token")]
    public async Task<IActionResult> GetConnectionToken()
    {
        var token = await tokenService.GenerateConnectionTokenAsync(ambientData);
        return Ok(new { connectionToken = token });
    }
}