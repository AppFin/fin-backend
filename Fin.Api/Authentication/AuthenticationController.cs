using System.Security.Claims;
using Fin.Application.Authentications.Dtos;
using Fin.Infrastructure.Authentications.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationService = Fin.Application.Authentications.Services.IAuthenticationService;

namespace Fin.Api.Authentication;

[Route("authentications")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginOutput>> Login([FromBody] LoginInput input)
    {
        var result = await _authenticationService.Login(input);
        if (result.Success)
            return Ok(result);
        return UnprocessableEntity(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<LoginOutput>> RefreshToken([FromBody] RefreshTokenInput input)
    {
        var result = await _authenticationService.RefreshToken(input.RefreshToken);
        if (result.Success)
            return Ok(result);
        return UnprocessableEntity(result);
    }

    [HttpPost("logged-out")]
    [Authorize]
    public async Task<ActionResult<LoginOutput>> LoggedOut()
    {
        var token = Request.Headers["Authorization"].ToString();
        token = token["Bearer ".Length..];

        await _authenticationService.Logout(token);
        return Ok();
    }

    [HttpGet("login-google")]
    public IActionResult LoginGoogle()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/authentications/google-callback", };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return Unauthorized();

        var loginResult = await _authenticationService.LoginOrSingInWithGoogle(new LoginWithGoogleInput
        {
            GoogleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            DisplayName = result.Principal.Identity?.Name,
            Email = result.Principal.FindFirst(ClaimTypes.Email)?.Value,
            FirstName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value,
            LastName = result.Principal.FindFirst(ClaimTypes.Surname)?.Value,
            PictureUrl = result.Principal.FindFirst("picture")?.Value,
        });

        if (!loginResult.Success)
            return UnprocessableEntity();

        return loginResult.MustToCreateUser ? Created("", loginResult) : Ok(loginResult);
    }

    [HttpPost("send-reset-password-email")]
    public async Task<ActionResult> StartResetPassword([FromBody] SendResetPasswordEmailInput input)
    {
        await _authenticationService.SendResetPasswordEmail(input);
        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordInput input)
    {
        var result = await _authenticationService.ResetPassword(input);
        if (result.Success)
            return Ok(result.Data);
        return UnprocessableEntity(result);
    }
}