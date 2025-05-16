using Fin.Application.Notifications;
using Fin.Domain.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Notifications;

[Authorize]
[Route("user-remember-use-settings")]
public class UserRememberUseSettingsController(IUserRememberUseSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserRememberUseSettingOutput>> Get()
    {
        return await service.GetByCurrentUser();   
    }
    
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] UserRememberUseSettingInput input)
    {
        var success = await service.UpdateByCurrentUser(input, true);   
        if (success) return Ok();
        return UnprocessableEntity();  
    }
}