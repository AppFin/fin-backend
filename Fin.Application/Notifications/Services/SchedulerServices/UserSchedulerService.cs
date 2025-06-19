using System.Collections.ObjectModel;
using Fin.Application.Notifications.Services.DeliveryServices;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.BackgroundJobs;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.Services.SchedulerServices;

public interface IUserSchedulerService
{
    public Task ScheduleDailyNotifications(CancellationToken cancellationToken = default);
    public Task ScheduleNotification(Notification notification, bool autosave = true, CancellationToken cancellationToken = default);
    public Task ScheduleNotification(Guid notificationId, bool autosave = true, CancellationToken cancellationToken = default);
    public Task UnscheduleNotification(Guid notificationId, List<Guid> userIds, CancellationToken cancellationToken = default);
}

public class UserSchedulerService(
    IRepository<Notification> notificationRepository,
    IRepository<NotificationUserDelivery> notificationUserRepository,
    IDateTimeProvider dateTimeProvider,
    IUserRememberUseSchedulerService rememberUseSchedulerService,
    IBackgroundJobManager backgroundJobManager
    ) : IUserSchedulerService, IAutoTransient
{
    public async Task ScheduleDailyNotifications(CancellationToken cancellationToken = default)
    {
        await rememberUseSchedulerService.ScheduleTodayNotification(autoSave: true);

        var startOfDay = dateTimeProvider.UtcNow().Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var notifications = await notificationRepository.Query()
            .Include(n => n.UserDeliveries)
            .Where(n => startOfDay <= n.StartToDelivery && n.StartToDelivery <= endOfDay)
            .Where(n => n.UserDeliveries.Any(n => !n.Delivery))
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0) return;

        foreach (var notification in notifications)
        {
            notification.UserDeliveries =
                new Collection<NotificationUserDelivery>(notification.UserDeliveries.Where(n => !n.Delivery).ToList());
            await ScheduleNotification(notification, false, cancellationToken);
        }
        await notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ScheduleNotification(Guid notificationId, bool autosave = true, CancellationToken cancellationToken = default)
    {
        var notification = await notificationRepository.Query(true)
            .Include(n => n.UserDeliveries).FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        await ScheduleNotification(notification, autosave, cancellationToken);
    }

    public async Task ScheduleNotification(Notification notification, bool autosave = true, CancellationToken cancellationToken = default)
    {
        foreach (var userDelivery in notification.UserDeliveries)
        {

            if (!string.IsNullOrWhiteSpace(userDelivery.BackgroundJobId))
            {
                backgroundJobManager.Delete(userDelivery.BackgroundJobId);
            }

            userDelivery.BackgroundJobId = backgroundJobManager.Schedule<INotificationDeliveryService>(
                service => service.SendNotification(new NotifyUserDto(notification, userDelivery), true),
                notification.StartToDelivery);
        }

        await notificationRepository.UpdateAsync(notification, autosave, cancellationToken);
    }

    public async Task UnscheduleNotification(Guid notificationId, List<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var jobsIds = await notificationUserRepository.Query(false)
            .Where(n => n.NotificationId == notificationId &&
                        userIds.Contains(n.UserId) &&
                        !string.IsNullOrWhiteSpace(n.BackgroundJobId))
            .Select(n => n.BackgroundJobId).ToListAsync(cancellationToken);

        foreach (var jobId in jobsIds)
        {
            backgroundJobManager.Delete(jobId);
        }
    }
}