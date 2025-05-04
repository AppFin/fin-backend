using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Services;
using Fin.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Authentication;

[Route("authentications/users/create")]
public class UserCreateController: ControllerBase
{
    private readonly IUserCreateService _userCreateService;

    public UserCreateController(IUserCreateService userCreateService)
    {
        _userCreateService = userCreateService;
    }

    [HttpPost("start")]
    public async Task<ActionResult<ValidationResultDto<UserStartCreateOutput>>> StartCreate([FromBody] UserStartCreateInput input)
    {
        var result = await _userCreateService.StartCreate(input);
        if (result.Success)
            return Ok(result);
        return UnprocessableEntity(result);
    }

    [HttpPost("resend-email")]
    public async Task<ActionResult<ValidationResultDto<DateTime>>> ResendEmail([FromQuery] string creationToken)
    {
        var result = await _userCreateService.ResendConfirmationEmail(creationToken);
        if (result.Success)
            return Ok(result);
        return UnprocessableEntity(result);
    }
}