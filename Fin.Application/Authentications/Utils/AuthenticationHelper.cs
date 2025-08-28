using System.Text.Json;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Authentications.Utils;

public interface IAuthenticationHelper
{
    public IActionResult GenerateCallbackResponse(bool success, string errorMessage, LoginOutput loginResult);
}

public class AuthenticationHelper(IConfiguration configuration): IAuthenticationHelper, IAutoTransient
{
    public IActionResult GenerateCallbackResponse(bool success, string errorMessage, LoginOutput loginResult)
    {
        var frontendOrigins = GetAllowedFrontendOrigins();

        if (success && loginResult != null)
        {
            var html = GenerateSuccessHtml(loginResult, frontendOrigins);
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }
        else
        {
            var html = GenerateErrorHtml(errorMessage, frontendOrigins);
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }
    }

    private string[] GetAllowedFrontendOrigins()
    {
        return [configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>()];
    }

    private string GenerateSuccessHtml(LoginOutput result, string[] allowedOrigins)
    {
        var jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var originsScript = string.Join(", ", allowedOrigins.Select(o => $"'{o}'"));

        return AuthenticationTemplates.ExternalLoginSuccessHtml
            .Replace("{{originsScript}}", originsScript)
            .Replace("{{jsonResult}}", jsonResult);
    }

    private string GenerateErrorHtml(string errorMessage, string[] allowedOrigins)
    {
        var originsScript = string.Join(", ", allowedOrigins.Select(o => $"'{o}'"));

        return AuthenticationTemplates.ExternalLoginFailHtml
            .Replace("{{originsScript}}", originsScript)
            .Replace("{{errorMessage}}", errorMessage);
    }
}