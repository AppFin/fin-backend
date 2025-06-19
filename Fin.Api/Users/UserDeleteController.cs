using Fin.Application.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Users;

[Route("users/delete")]
[Authorize]
public class UserDeleteController(IUserDeleteService userDeleteService) : ControllerBase
{
    [HttpPost("request")]
    public async Task<ActionResult<bool>> RequestDelete(CancellationToken cancellationToken)
    {
        var result = await userDeleteService.RequestDeleteUser(cancellationToken);
        if (result) return Ok(result);
        return BadRequest("Unable to process delete request. User may not exist or already has a pending deletion request.");
    }

    [HttpPost("abort/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<bool>> AbortDelete([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var result = await userDeleteService.AbortDeleteUser(userId, cancellationToken);
        if (result) return Ok(result);
        return NotFound("Delete request not found or already processed.");
    }
}