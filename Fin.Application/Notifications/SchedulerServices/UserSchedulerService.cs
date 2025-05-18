using System.Collections.ObjectModel;
using Fin.Application.Notifications.DeliveryServices;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.SchedulerServices;

public interface IUserSchedulerService
{
    public Task ScheduleDailyNotifications();
}

public class UserSchedulerService(
    IRepository<Notification> notificationRepository,
    DateTimeProvider dateTimeProvider) : IUserSchedulerService, IAutoTransient
{
    public async Task ScheduleDailyNotifications()
    {
        var startOfDay = dateTimeProvider.UtcNow().Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var notifications = await notificationRepository.Query(false)
            .Include(n => n.UserDeliveries)
            .Where(n => startOfDay <= n.StartToDelivery && n.StartToDelivery <= endOfDay)
            .Where(n => n.UserDeliveries.Any(n => !n.Delivery))
            .ToListAsync();

        if (notifications.Count == 0) return;

        foreach (var notification in notifications)
        {
            notification.UserDeliveries =
                new Collection<NotificationUserDelivery>(notification.UserDeliveries.Where(n => !n.Delivery).ToList());
            SchedulerNotification(notification);
        }
    }

    private void SchedulerNotification(Notification notification)
    {
        foreach (var userDelivery in notification.UserDeliveries)
        {
            BackgroundJob.Schedule<INotificationDeliveryService>(
                "",
                service => service.SendNotification(new NotifyUserDto(notification, userDelivery)),
                notification.StartToDelivery);
        }
    }
}