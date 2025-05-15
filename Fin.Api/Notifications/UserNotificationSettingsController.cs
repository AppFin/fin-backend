using Fin.Application.Notifications;
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
        var success = await service.UpdateByCurrentUser(input);   
        if (success) return Ok();
        return UnprocessableEntity();  
    }
}