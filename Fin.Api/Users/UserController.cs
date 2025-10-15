using Fin.Application.Users.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Enums;
using Fin.Infrastructure.Authentications.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Users;

[Route("users")]
[Authorize(Roles = AuthenticationRoles.Admin)]
public class UserController(IUserService service): ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get([FromRoute] Guid id)
    {
        var menu = await service.Get(id);
        return menu != null ? Ok(menu) : NotFound();   
    }
    
    [HttpGet]
    public async Task<PagedOutput<UserDto>> GetList([FromQuery] PagedFilteredAndSortedInput input)
    {
        return await service.GetList(input);
    }

    [HttpPatch("{id:guid}/theme")]
    public async Task<ActionResult<UserDto>> UpdateTheme([FromRoute] Guid id, [FromBody] string theme)
    {
        var user = await service.UpdateTheme(id, theme, autoSave: true);
        return Ok(user);
    }
}