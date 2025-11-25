using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.AutoServices.Extensions;
using Fin.Infrastructure.BackgroundJobs;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.EmailSenders.MailSender;
using Fin.Infrastructure.Firebases;
using Fin.Infrastructure.Notifications.Hubs;
using Fin.Infrastructure.Redis;
using Fin.Infrastructure.Seeders.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Extensions;

public static class AddInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddRedis(configuration)
            .AddFinAuthentication(configuration)
            .AddAutoServices()
            .AddBackgroundJobs(configuration)
            .AddScoped<TokenBlacklistMiddleware>()
            .AddScoped<ActivatedMiddleware>()
            .AddScoped<AmbientDataMiddleware>()
            .AddDatabase(configuration)
            .AddFirebase(configuration)
            .AddSeeders()
            .AddNotifications()
            .AddMailSenderClient();

        return services;
    }
}