using Fin.Infrastructure.Database.Interceptors;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Database.Extensions;

public static class AddDatabaseExtension
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddScoped<AuditedEntityInterceptor>()
            .AddScoped<TenantEntityInterceptor>()
            .AddDbContext<FinDbContext>((serviceProvider, op) =>
            {
                var auditedEntityInterceptor = serviceProvider.GetRequiredService<AuditedEntityInterceptor>();
                var tenantEntityInterceptor = serviceProvider.GetRequiredService<TenantEntityInterceptor>();
                
                op
                    .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                    .AddInterceptors(auditedEntityInterceptor)
                    .AddInterceptors(tenantEntityInterceptor);
            })
            .AddScoped(typeof(IRepository<>), typeof(Repository<>));;
        
        return services;
    }

    public static async Task UseDbMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FinDbContext>>();
        try
        {
            logger.LogInformation("Applying migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred while applying migrations: {message}", ex.Message);
            throw;
        }
    }
}