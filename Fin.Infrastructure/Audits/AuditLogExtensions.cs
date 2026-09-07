using Fin.Infrastructure.Audits.Interfaces;
using Fin.Infrastructure.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Fin.Infrastructure.Audits;

public static class AuditLogExtensions
{
    public static IServiceCollection AddAuditLog(this IServiceCollection services, IConfiguration configuration)
    {
        // Portfolio demo mode has no MongoDB container running (saves resources on
        // the small VM) - skip it entirely instead of opening a connection nobody reads.
        if (configuration.GetValue<bool>(AppConstants.DemoModeConfigKey))
        {
            services.AddScoped<IAuditLogService, NullAuditLogService>();
            services.AddScoped<AuditLogInterceptor>();
            return services;
        }

        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
        }
        catch (BsonSerializationException) {}

        var settings = MongoClientSettings.FromConnectionString(configuration.GetConnectionString("MongoDbConnection"));
        var mongoClient = new MongoClient(settings);

        services.AddSingleton<IMongoClient>(_ => mongoClient);
        services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("LogsDB"));
        services.AddScoped<IAuditLogService, MongoAuditLogService>();

        services.AddScoped<AuditLogInterceptor>();


        return services;
    }
}