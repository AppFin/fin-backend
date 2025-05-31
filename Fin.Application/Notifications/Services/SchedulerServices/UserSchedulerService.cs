using System.Collections.ObjectModel;
using Fin.Application.Notifications.Services.DeliveryServices;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.Services.SchedulerServices;

public interface IUserSchedulerService
{
    public Task ScheduleDailyNotifications();
    public void ScheduleNotification(Notification notification);
    public void UnscheduleNotification(Guid notificationId, List<Guid> userIds);
}

public class UserSchedulerService(
    IRepository<Notification> notificationRepository,
    IDateTimeProvider dateTimeProvider,
    IRecurringJobManager recurringJobManager) : IUserSchedulerService, IAutoTransient
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
            ScheduleNotification(notification);
        }
    }

    public void ScheduleNotification(Notification notification)
    {
        foreach (var userDelivery in notification.UserDeliveries)
        {
            var jobId = GetJobIb(notification.Id, userDelivery.UserId);;

            BackgroundJob.Delete(jobId);
            BackgroundJob.Schedule<INotificationDeliveryService>(
                jobId,
                service => service.SendNotification(new NotifyUserDto(notification, userDelivery), true),
                notification.StartToDelivery);
        }
    }

    public void UnscheduleNotification(Guid notificationId, List<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            BackgroundJob.Delete(GetJobIb(notificationId, userId));
        }
    }

    private string GetJobIb(Guid notificationId, Guid userId)
    {
        return $"notification:{notificationId}/user:{userId}";
    }
}