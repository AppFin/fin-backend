using Fin.Application.Users.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Users.Dtos;
using Fin.Infrastructure.Authentications.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Users;

[Route("users")]
[Authorize(Roles = AuthenticationRoles.Admin)]
public class UserController(IUserService service): ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AuthenticationRoles.Admin)]
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
}