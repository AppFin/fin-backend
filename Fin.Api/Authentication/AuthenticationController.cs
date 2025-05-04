using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Authentications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<LoginOutput>> RefreshToken([FromBody] string refreshToken)
    {
        var result = await _authenticationService.RefreshToken(refreshToken);
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
}