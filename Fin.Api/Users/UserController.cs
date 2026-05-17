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
    public async Task<ActionResult<UserDto>> Get([FromRoute] Guid id)
    {
        var menu = await service.Get(id);
        return menu != null ? Ok(menu) : NotFound();   
    }
    
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] UserUpdateOrCreateInput input, CancellationToken token)
    {
        await service.Update(id, input, token);
        return Ok();   
    }
    
    [HttpGet]
    public async Task<PagedOutput<UserDto>> GetList([FromQuery] PagedFilteredAndSortedInput input)
    {
        return await service.GetList(input);
    }
}