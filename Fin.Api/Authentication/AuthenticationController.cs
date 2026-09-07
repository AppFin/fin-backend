using System.Security.Claims;
using System.Text.Json;
using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Utils;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using IAuthenticationService = Fin.Application.Authentications.Services.IAuthenticationService;

namespace Fin.Api.Authentication;

[Route("authentications")]
[EnableRateLimiting("auth")]
public class AuthenticationController(
    IAuthenticationService authenticationService,
    IAuthenticationHelper helper,
    IConfiguration configuration) : ControllerBase
{
    private bool IsDemoMode => configuration.GetValue<bool>(AppConstants.DemoModeConfigKey);

    private const string DemoModeDisabledMessage = "This feature is disabled in the portfolio demo.";
    [HttpPost("login")]
    public async Task<ActionResult<LoginOutput>> Login([FromBody] LoginInput input)
    {
        var result = await authenticationService.Login(input);
        if (result.Success)
            return Ok(result);
        return UnprocessableEntity(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<LoginOutput>> RefreshToken([FromBody] RefreshTokenInput input)
    {
        var result = await authenticationService.RefreshToken(input.RefreshToken);
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

        await authenticationService.Logout(token);
        return Ok();
    }

    [HttpGet("login-google")]
    public IActionResult LoginGoogle([FromQuery] string state = null)
    {
        if (IsDemoMode)
            return NotFound(DemoModeDisabledMessage);

        if (string.IsNullOrEmpty(state))
        {
            state = Guid.NewGuid().ToString("N");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = "/authentications/google-callback",
            Items = { ["state"] = state }
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        if (IsDemoMode)
            return NotFound(DemoModeDisabledMessage);

        try
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return helper.GenerateCallbackResponse(false, "Authentication failed", null);

            var loginResult = await authenticationService.LoginOrSingInWithGoogle(new LoginWithGoogleInput
            {
                GoogleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                DisplayName = result.Principal.Identity?.Name,
                Email = result.Principal.FindFirst(ClaimTypes.Email)?.Value,
                FirstName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value,
                LastName = result.Principal.FindFirst(ClaimTypes.Surname)?.Value,
                PictureUrl = result.Principal.FindFirst("picture")?.Value,
            });

            if (!loginResult.Success)
                return helper.GenerateCallbackResponse(false, "Internal authentication failed", loginResult);

            return helper.GenerateCallbackResponse(true, null, loginResult);
        }
        catch (Exception ex)
        {
            return helper.GenerateCallbackResponse(false, "Internal authentication failed", null);
        }
    }

    [HttpPost("send-reset-password-email")]
    public async Task<ActionResult> StartResetPassword([FromBody] SendResetPasswordEmailInput input)
    {
        if (IsDemoMode)
            return NotFound(DemoModeDisabledMessage);

        await authenticationService.SendResetPasswordEmail(input);
        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordInput input)
    {
        if (IsDemoMode)
            return NotFound(DemoModeDisabledMessage);

        var result = await authenticationService.ResetPassword(input);
        if (result.Success)
            return Ok(result.Data);
        return UnprocessableEntity(result);
    }
}