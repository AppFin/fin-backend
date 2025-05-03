using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Extensions;

public static class AddInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddInfrastructureServices()
            .AddDatabase(configuration);
        
        return services;
    }
}