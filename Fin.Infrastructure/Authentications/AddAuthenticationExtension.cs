using System.Text;
using Fin.Infrastructure.Authentications.Consts;
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
                options.ClientId = configuration.GetSection(AuthenticationConsts.GoogleClientIdConfigKey).Value ?? "";
                options.ClientSecret = configuration.GetSection(AuthenticationConsts.GoogleClientSecretConfigKey).Value ?? "";
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
                    ValidIssuer = configuration.GetSection(AuthenticationConsts.TokenJwtIssuerConfigKey).Value ?? "",
                    ValidAudience = configuration.GetSection(AuthenticationConsts.TokenJwtAudienceConfigKey).Value ?? "",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetSection(AuthenticationConsts.TokenJwtKeyConfigKey).Value ?? ""))
                };
            });
        services.AddAuthorization();

        return services;
    }
}