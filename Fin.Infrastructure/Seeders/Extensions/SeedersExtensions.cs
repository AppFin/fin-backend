using Fin.Infrastructure.AutoServices.Extensions;
using Fin.Infrastructure.Seeders.interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ISeeder>>();

        try
        {
            logger.LogInformation("Seeders started");
            foreach (var seeder in seeders)
            {
                await seeder.SeedAsync();
            }
            logger.LogInformation("Seeders finished");
        }
        catch (Exception ex)
        {
            var isRelationDoesNotExistError = ex.Message.Contains("42P01");
            logger.LogError(
                isRelationDoesNotExistError
                    ? "Error \"relation X does not exist\" was thrown. Did you run the migrations? Error: \n{message}"
                    : "An error occurred while seeding: {message}", ex.Message);
            throw;
        }
    }
    
}