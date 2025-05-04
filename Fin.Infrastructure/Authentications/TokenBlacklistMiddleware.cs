using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Redis;
using Microsoft.AspNetCore.Http;

namespace Fin.Infrastructure.Authentications;

public class TokenBlacklistMiddleware: IMiddleware, IAutoScoped
{
    private readonly IRedisCacheService _cache;

    public TokenBlacklistMiddleware(IRedisCacheService cache)
    {
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader["Bearer ".Length..];

            var isLoggedOut = await _cache.GetAsync<bool>(AuthenticationService.GetLogoutTokenCacheKey(token));

            if (isLoggedOut)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token has been logged out.");
                return;
            }
        }

        await next(context);
    }
}