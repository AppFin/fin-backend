using Fin.Application.Notifications.SchedulerServices;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Application.Notifications.Extensions;

public static class UseNotificationBackgroundJob
{
    public static void UseNotifications(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<IUserSchedulerService>(
            NotificationBackgroundJobConsts.MidNightJobName,
            service => service.ScheduleDailyNotifications(),
            Cron.Daily(0)
        );
    }
}