using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Authentications;

public class ActivatedMiddleware(IAmbientData ambientData, IRepository<User> userRepo): IMiddleware
{

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (ambientData.IsLogged)
        {
            var userExistAndActivated = await userRepo.AsNoTracking().AnyAsync(u => u.IsActivity && u.Id == ambientData.UserId);

            if (!userExistAndActivated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid user, deleted or inactive user");
                return;
            }
        }

        await next(context);
    }
}