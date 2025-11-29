using System.IdentityModel.Tokens.Jwt;
using Fin.Infrastructure.Authentications.Constants;
using Microsoft.AspNetCore.Http;

namespace Fin.Infrastructure.AmbientDatas;

public class AmbientDataMiddleware(IAmbientData ambientData) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader["Bearer ".Length..];

            var handler = new JwtSecurityTokenHandler();

            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);

                var displayName = jwt.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value ?? "";
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value ?? "";
                var isAdmin = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value == AuthenticationRoles.Admin;
                var tenantId = jwt.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value ?? "";

                ambientData.SetData(Guid.Parse(tenantId), Guid.Parse(userId), displayName, isAdmin);           
            }
        }
        else
        {
            ambientData.SetNotLogged();
        }

        await next(context);
    }
}