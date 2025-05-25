using Fin.Application.Notifications.CrudServices;
using Fin.Domain.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Notifications;

[Authorize]
[Route("user-notification-settings")]
public class UserNotificationSettingsController(IUserNotificationSettingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserNotificationSettingsOutput>> Get()
    {
        return await service.GetByCurrentUser();   
    }
    
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] UserNotificationSettingsInput input)
    {
        var success = await service.UpdateByCurrentUser(input, true);   
        if (success) return Ok();
        return UnprocessableEntity();  
    }

    [HttpPost("add-firebase-token/{token}")]
    public async Task<ActionResult> AddFirebaseToken([FromRoute] string token)
    {
        var success = await service.AddFirebaseToken(token, true);
        if (success) return Ok();
        return UnprocessableEntity();
    }
}