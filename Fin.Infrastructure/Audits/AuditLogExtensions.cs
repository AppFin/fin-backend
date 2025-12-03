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
        var settings = MongoClientSettings.FromConnectionString(configuration.GetConnectionString("MongoDbConnection"));
        var mongoClient = new MongoClient(settings);
        
        services.AddSingleton<IMongoClient>(_ => mongoClient);
        services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("LogsDB"));
        services.AddScoped<IAuditLogService, MongoAuditLogService>();
        
        services.AddScoped<AuditLogInterceptor>();
        
        
        return services;
    }
}