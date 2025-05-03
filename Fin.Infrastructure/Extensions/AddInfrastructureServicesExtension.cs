using Fin.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Extensions;

public static class AddInfrastructureServicesExtension
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IDateTimeProvider, DateTimeProvider>();
        
        return services;
    }
}