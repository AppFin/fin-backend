using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Notifications;

public static class AddNotificationBackgroundJob
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        RecurringJob.AddOrUpdate<INotificationBackgroundJobService>(
            NotificationBackgroundJobConsts.MidNightJobName,
            service => service.ScheduleDailyNotifications(),
            Cron.Daily(0)
            );

        return services;
    }
}