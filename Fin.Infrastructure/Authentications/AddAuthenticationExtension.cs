using System.Text;
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
                options.ClientId = configuration.GetSection("ApiSettings:Authentication:Google:ClientId").Value ?? "";
                options.ClientSecret = configuration.GetSection("ApiSettings:Authentication:Google:ClientSecret").Value ?? "";
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
                    ValidIssuer = configuration.GetSection("ApiSettings:Authentication:Jwt:Issuer").Value ?? "",
                    ValidAudience = configuration.GetSection("ApiSettings:Authentication:Jwt:Audience").Value ?? "",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration.GetSection("ApiSettings:Authentication:Jwt:Key").Value ??
                                               ""))
                };
            });
        services.AddAuthorization();

        return services;
    }
}