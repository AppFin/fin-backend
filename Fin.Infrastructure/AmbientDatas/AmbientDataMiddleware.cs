using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fin.Infrastructure.AutoServices.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Fin.Infrastructure.AmbientDatas;

public class AmbientDataMiddleware: IMiddleware, IAutoScoped
{
    private readonly IAmbientData _ambientData;

    public AmbientDataMiddleware(IAmbientData ambientData)
    {
        _ambientData = ambientData;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader["Bearer ".Length..];

            var handler = new JwtSecurityTokenHandler();

            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);

                var displayName = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "";
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? "";
                var isAdmin = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value == "Admin";
                var tenantId = jwt.Claims.FirstOrDefault(c => c.Type == "TenantId")?.Value ?? "";

                _ambientData.SetData(Guid.Parse(tenantId), Guid.Parse(userId), displayName, isAdmin);           
            }
        }

        await next(context);
    }
}