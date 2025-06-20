using Fin.Application.Notifications.Services.SchedulerServices;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.BackgroundJobs;
using Hangfire;

namespace Fin.Application.Notifications.BackgroundJobs;

public class NotificationScheduleBackgroundJob(IUserSchedulerService schedulerService): IAsyncRecurringBackgroundJob
{
    public string CronExpression => Cron.Daily(0, 0);
    public string RecurringJobId => "MidNightNotificationJob";
    public async Task ExecuteAsync()
    {
        await schedulerService.ScheduleDailyNotifications();
    }
}