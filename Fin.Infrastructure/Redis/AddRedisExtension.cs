using Fin.Infrastructure.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Fin.Infrastructure.Redis;

public static class AddRedisExtension
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("ApiSettings:Redis").Value ?? "";

        services
            .AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(connectionString))
            .AddStackExchangeRedisCache(op =>
            {
                op.Configuration = connectionString;
                op.InstanceName = AppConstants.AppName;
            });
        
        return services;
    }
}