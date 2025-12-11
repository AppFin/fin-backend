#nullable enable
using System.Security.Claims;
using System.Text;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.Notifications.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Fin.Infrastructure.Authentications;

public static class AddAuthenticationExtension
{
    public static IServiceCollection AddFinAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle("Google", options =>
            {
                options.ClientId = configuration.GetSection(AuthenticationConstants.GoogleClientIdConfigKey).Value ??
                                   "";
                options.ClientSecret =
                    configuration.GetSection(AuthenticationConstants.GoogleClientSecretConfigKey).Value ?? "";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.CallbackPath = "/authentications/google-sign-callback";

                options.ClaimActions.MapJsonKey("picture", "picture", "url");
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration.GetSection(AuthenticationConstants.TokenJwtIssuerConfigKey).Value ?? "",
                    ValidAudience = configuration.GetSection(AuthenticationConstants.TokenJwtAudienceConfigKey).Value ??
                                    "",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        configuration.GetSection(AuthenticationConstants.TokenJwtKeyConfigKey).Value ?? ""))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        if (!path.StartsWithSegments("/notifications-hub")) return;

                        var connectionToken = context.Request.Query["access_token"].ToString();

                        if (string.IsNullOrEmpty(connectionToken))
                        {
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            if (!string.IsNullOrEmpty(authHeader) &&  authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                connectionToken = authHeader.Substring("Bearer ".Length).Trim();
                            } else if (!string.IsNullOrEmpty(authHeader))
                            {
                                connectionToken = authHeader;
                            }
                        }
                        
                        if (string.IsNullOrEmpty(connectionToken))
                        {
                            context.Fail("Connection token is required");
                            return;
                        }

                        var tokenService = context.HttpContext.RequestServices.GetRequiredService<IWebSocketTokenService>();
                        var tokenData = await tokenService.ValidateAndConsumeTokenAsync(connectionToken);

                        if (tokenData == null)
                        {
                            context.Fail("Invalid or expired connection token");
                            return;
                        }

                        var claims = new[]
                        {
                            new Claim("userId", tokenData.UserId.ToString()),
                            new Claim("tenantId", tokenData.TenantId.ToString()),
                            new Claim("displayName", tokenData.DisplayName),
                            new Claim("isAdmin", tokenData.IsAdmin.ToString()),
                            new Claim(ClaimTypes.NameIdentifier, tokenData.UserId.ToString())
                        };

                        var identity = new ClaimsIdentity(claims, "WebSocketToken");
                        context.Principal = new ClaimsPrincipal(identity);
                        context.Success();
                    }
                };
            });
        services.AddAuthorization();

        return services;
    }
}