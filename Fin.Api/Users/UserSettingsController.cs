using Fin.Application.Users.Services;
using Fin.Domain.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Users;

[Route("user-settings")]
[Authorize]
public class UserSettingsController(IUserSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> Get()
    {
        var result = await service.Get();
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPut]
    public async Task<ActionResult<UserSettingsDto>> Update([FromBody] UserSettingsUpdateInput input)
    {
        var result = await service.Update(input);
        return Ok(result);
    }
}

