using Fin.Infrastructure.AutoServices.Extensions;
using Fin.Infrastructure.Database.Extensions;
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
            .AddAutoServices()
            .AddDatabase(configuration);
        
        return services;
    }
}