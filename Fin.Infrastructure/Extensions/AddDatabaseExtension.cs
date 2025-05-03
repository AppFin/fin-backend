using Fin.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Extensions;

public static class AddDatabaseExtension
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDbContext<FinDbContext>((serviceProvider, op) =>
            {
                
                var auditedEntityInterceptor = serviceProvider.GetRequiredService<AuditedEntityInterceptor>();
                
                op
                    .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                    .AddInterceptors(auditedEntityInterceptor);
            });
        
        return services;
    }
}