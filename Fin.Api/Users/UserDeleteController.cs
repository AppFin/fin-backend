using Fin.Application.Users.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Users;

[Route("users/delete")]
[Authorize]
public class UserDeleteController(IUserDeleteService userDeleteService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedOutput<UserDeleteRequestDto>> GetList([FromQuery] PagedFilteredAndSortedInput input, CancellationToken cancellationToken)
    {
        return await userDeleteService.GetList(input, cancellationToken);
    }

    [HttpPost("abort/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<bool>> AbortDelete([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var result = await userDeleteService.AbortDeleteUser(userId, cancellationToken);
        if (result) return Ok(result);
        return NotFound("Delete request not found or already processed.");
    }

    [HttpPost("request")]
    public async Task<ActionResult<bool>> RequestDelete(CancellationToken cancellationToken)
    {
        var result = await userDeleteService.RequestDeleteUser(cancellationToken);
        if (result) return Ok(result);
        return BadRequest("Unable to process delete request. User may not exist or already has a pending deletion request.");
    }
}