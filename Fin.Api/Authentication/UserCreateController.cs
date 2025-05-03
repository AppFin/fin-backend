using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Enums;
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
    public async Task<ActionResult<ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>>> StartCreate(UserStartCreateInput input)
    {
        var result = await _userCreateService.StartCreate(input);
        if (result.Success)
            return Ok(result);
        return UnprocessableEntity(result);
    }
}