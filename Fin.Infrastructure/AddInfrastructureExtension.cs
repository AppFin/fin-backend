using Fin.Infrastructure.AutoServices.Extensions;
using Fin.Infrastructure.Database.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure;

public static class AddInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAutoServices()
            .AddDatabase(configuration);
        
        return services;
    }
}