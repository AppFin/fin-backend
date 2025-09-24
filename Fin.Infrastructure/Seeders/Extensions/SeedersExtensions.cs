using Fin.Infrastructure.AutoServices.Extensions;
using Fin.Infrastructure.Seeders.interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Seeders.Extensions;

public static class SeedersExtensions
{
    public static IServiceCollection AddSeeders(this IServiceCollection services)
    {
        AddAutoServicesExtension.RegisterDependencyByType(services, typeof(ISeeder), ServiceLifetime.Transient);
        return services;
    }

    public static async Task UseSeeders(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<ISeeder>();
        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync();
        }
    }
    
}