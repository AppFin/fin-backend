using Fin.Infrastructure.Database.IRepositories;
using Fin.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Database.Extensions;

public static class AddDatabaseExtension
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddScoped<AuditedEntityInterceptor>()
            .AddDbContext<FinDbContext>((serviceProvider, op) =>
            {
                var auditedEntityInterceptor = serviceProvider.GetRequiredService<AuditedEntityInterceptor>();
                
                op
                    .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                    .AddInterceptors(auditedEntityInterceptor);
            })
            .AddScoped(typeof(IRepository<>), typeof(Repository<>));;
        
        services.AddHostedService<MigrateDatabaseHostedService>();
        
        
        return services;
    }
}