using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.BackgroundJobs;

public static class AddBackgroundJobExtension
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services
            .AddHangfire(x => x.UseSqlServerStorage("DefaultConnection"))
            .AddHangfireServer();

        return services;
    }
}