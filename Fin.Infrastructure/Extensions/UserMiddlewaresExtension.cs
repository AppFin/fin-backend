using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Errors;
using Microsoft.AspNetCore.Builder;

namespace Fin.Infrastructure.Extensions;

public static class UserMiddlewaresExtension
{
    public static WebApplication UseFinMiddlewares(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<TokenBlacklistMiddleware>();
        app.UseMiddleware<AmbientDataMiddleware>();
        app.UseMiddleware<ActivatedMiddleware>();

        return app;
    }
}