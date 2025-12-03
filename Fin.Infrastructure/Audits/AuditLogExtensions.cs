using Fin.Infrastructure.Audits.Enums;
using Fin.Infrastructure.Audits.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Fin.Infrastructure.Audits;

public static class AuditLogExtensions
{
    public static IServiceCollection AddAuditLog(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMongoClient>(_ => new MongoClient(configuration.GetConnectionString("MongoDbConnection")));
        services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("LogsDB"));
        services.AddScoped<IAuditLogService, MongoAuditLogService>();
        
        services.AddScoped<AuditLogInterceptor>();
        
        return services;
    }
}