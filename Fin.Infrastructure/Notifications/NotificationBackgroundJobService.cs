using Fin.Infrastructure.AutoServices.Interfaces;

namespace Fin.Infrastructure.Notifications;

public interface INotificationBackgroundJobService
{
    public Task ScheduleDailyNotifications();
}

public class NotificationBackgroundJobService: INotificationBackgroundJobService, IAutoSingleton
{
    public async Task ScheduleDailyNotifications()
    {
        await Task.CompletedTask;
    }
}