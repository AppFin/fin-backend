using Fin.Application.Notifications.SchedulerServices;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Application.Notifications.Extensions;

public static class AddNotificationBackgroundJob
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        RecurringJob.AddOrUpdate<IUserSchedulerService>(
            NotificationBackgroundJobConsts.MidNightJobName,
            service => service.ScheduleDailyNotifications(),
            Cron.Daily(0)
            );

        return services;
    }
}