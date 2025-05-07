using Fin.Application.Users.Dtos;
using Fin.Application.Users.Services;
using Fin.Domain.Users.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Users;

[Route("users/create")]
public class UserCreateController : ControllerBase
{
    private readonly IUserCreateService _userCreateService;

    public UserCreateController(IUserCreateService userCreateService)
    {
        _userCreateService = userCreateService;
    }

    [HttpPost("start")]
    public async Task<ActionResult<UserStartCreateOutput>> StartCreate([FromBody] UserStartCreateInput input)
    {
        var result = await _userCreateService.StartCreate(input);
        if (result.Success)
            return Ok(result.Data);
        return UnprocessableEntity(result);
    }

    [HttpPost("resend-email")]
    public async Task<ActionResult<DateTime>> ResendEmail([FromQuery] string creationToken)
    {
        try
        {
            var result = await _userCreateService.ResendConfirmationEmail(creationToken);
            if (result.Success)
                return Ok(result.Data);
            return UnprocessableEntity(result);
        }
        catch (UnauthorizedAccessException e)
        {
            return Unauthorized(e.Message);
        }
    }

    [HttpPost("valid-email")]
    public async Task<ActionResult<bool>> ValidEmail([FromQuery] string creationToken,
        [FromQuery] string emailCode)
    {
        try
        {
            var result = await _userCreateService.ValidateEmailCode(creationToken, emailCode);
            ;
            if (result.Success)
                return Ok(result.Data);
            return UnprocessableEntity(result);
        }
        catch (UnauthorizedAccessException e)
        {
            return Unauthorized(e.Message);
        }
    }

    [HttpPost("create-user")]
    public async Task<ActionResult<UserDto>> CreateUser([FromQuery] string creationToken,
        [FromBody] UserUpdateOrCreateInput input)
    {
        try
        {
            var result = await _userCreateService.CreateUser(creationToken, input);
            if (result.Success && result.Data != null)
                return Created($"users/${result.Data.Id}", result.Data);
            return UnprocessableEntity(result);
        }
        catch (UnauthorizedAccessException e)
        {
            return Unauthorized(e.Message);
        }
    }
}