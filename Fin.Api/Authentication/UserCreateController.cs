using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Enums;
using Fin.Application.Authentications.Services;
using Fin.Application.Dtos;
using Fin.Domain.Users.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Authentication;

[Route("authentications/users/create")]
public class UserCreateController : ControllerBase
{
    private readonly IUserCreateService _userCreateService;

    public UserCreateController(IUserCreateService userCreateService)
    {
        _userCreateService = userCreateService;
    }

    [HttpPost("start")]
    public async Task<ActionResult<UserStartCreateOutput>> StartCreate(
        [FromBody] UserStartCreateInput input)
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
    public async Task<ActionResult<UserOutput>> CreateUser([FromQuery] string creationToken,
        [FromBody] UserUpdateOrCreateDto input)
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
    
    [HttpPost("start-reset-password")]
    public async Task<ActionResult> StartResetPassword([FromBody] UserStartResetPasswordInput input)
    {
        await _userCreateService.StartResetPassword(input.Email);
        return Ok();
    }
    
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] UserResetPasswordInput input)
    {
        var result = await _userCreateService.ResetPassword(input);
        if (result.Success)
            return Ok(result.Data);
        return UnprocessableEntity(result);
    }
}