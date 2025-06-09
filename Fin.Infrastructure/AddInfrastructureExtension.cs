using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.AutoServices.Extensions;
using Fin.Infrastructure.BackgroundJobs;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Firebases;
using Fin.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure;

public static class AddInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddRedis(configuration)
            .AddFinAuthentication(configuration)
            .AddBackgroundJobs(configuration)
            .AddAutoServices()
            .AddScoped<TokenBlacklistMiddleware>()
            .AddScoped<AmbientDataMiddleware>()
            .AddDatabase(configuration)
            .AddFirebase(configuration);

        return services;
    }
}