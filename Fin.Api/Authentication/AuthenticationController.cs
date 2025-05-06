using System.Security.Claims;
using Fin.Application.Authentications.Dtos;
using Fin.Infrastructure.Authentications.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationService = Fin.Infrastructure.Authentications.IAuthenticationService;

namespace Fin.Api.Authentication;

[Route("authentications")]
public class AuthenticationController: ControllerBase
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
    public async Task<ActionResult<LoginOutput>> RefreshToken([FromBody] UserRefreshTokenInput input)
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
        var properties = new AuthenticationProperties { RedirectUri = "/authentications/google-callback",  };
        return Challenge(properties,  GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("Google");
        if (!result.Succeeded)
            return Unauthorized();

        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal.Identity?.Name;
        var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var loginResult = await _authenticationService.LoginOrSingInWithGoogle(name, email, googleId);

        if (loginResult.Success)
        {
            return loginResult.MustToCreateUser ? Created("", loginResult) : Ok(loginResult);
        }
        await HttpContext.SignOutAsync();
        return UnprocessableEntity();
    }
}