using Fin.Application.Notifications.Services.SchedulerServices;
using Fin.Domain.Global.Classes;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.Services.CrudServices;

public interface INotificationService
{
    public Task<NotificationOutput> Get(Guid id);
    public Task<PagedOutput<NotificationOutput>> GetList(PagedFilteredAndSortedInput input);
    public Task<NotificationOutput> Create(NotificationInput input, bool autoSave = false);
    public Task<bool> Update(Guid id, NotificationInput input, bool autoSave = false);
    public Task<bool> Delete(Guid id, bool autoSave = false);
}

public class NotificationService(
    IRepository<Notification> repository,
    IRepository<NotificationUserDelivery> deliveriesRepository,
    IDateTimeProvider dateTimeProvider,
    IUserSchedulerService _schedulerService
    ) : INotificationService, IAutoTransient
{
    public async Task<NotificationOutput> Get(Guid id)
    {
        var entity = await repository.Query()
            .FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new NotificationOutput(entity) : null;
    }

    public async Task<PagedOutput<NotificationOutput>> GetList(PagedFilteredAndSortedInput input)
    {
        return await repository.Query(false)
            .ApplyFilterAndSorter(input)
            .Select(n => new NotificationOutput(n))
            .ToPagedResult(input);
    }

    public async Task<NotificationOutput> Create(NotificationInput input, bool autoSave = false)
    {
        var notification = new Notification(input);
        await repository.AddAsync(notification, autoSave);

        var forToday = IsNotificationForToday(notification.StartToDelivery);
        if (forToday)
            _schedulerService.ScheduleNotification(notification);

        return new NotificationOutput(notification);
    }

    public async Task<bool> Update(Guid id, NotificationInput input, bool autoSave = false)
    {
        var notification = await repository.Query()
            .Include(u => u.UserDeliveries)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (notification == null) return false;

        var isOldForToday = IsNotificationForToday(notification.StartToDelivery);
        var isNewForToday = IsNotificationForToday(input.StartToDelivery);

        var oldDeliveries = notification.UserDeliveries.Select(d => d.UserId).ToList();

        var toDeleteDeliveries = notification.UpdateAndReturnToRemoveDeliveries(input);
        var toDeleteDeliveriesIds = toDeleteDeliveries.Select(d => d.UserId).ToList();

        await repository.UpdateAsync(notification);
        await deliveriesRepository.Query()
            .Where(d => d.NotificationId == notification.Id && toDeleteDeliveriesIds.Contains(d.UserId))
            .ExecuteDeleteAsync();

        if (autoSave)
            await repository.SaveChangesAsync();

        if (isOldForToday && !isNewForToday)
            _schedulerService.UnscheduleNotification(notification.Id, oldDeliveries);
        else if (!isOldForToday && isNewForToday)
            _schedulerService.ScheduleNotification(notification);
        else if (isOldForToday && isNewForToday)
        {
            _schedulerService.UnscheduleNotification(notification.Id, toDeleteDeliveriesIds);
            _schedulerService.ScheduleNotification(notification);
        }
        
        return true;   
    }

    public async Task<bool> Delete(Guid id, bool autoSave = false)
    {
        var notification = await repository.Query()
            .Include(n => n.UserDeliveries)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (notification == null) return false;

        var forToday = IsNotificationForToday(notification.StartToDelivery);
        if (forToday)
            _schedulerService.UnscheduleNotification(notification.Id, notification.UserDeliveries.Select(d => d.UserId).ToList());

        await repository.DeleteAsync(notification, autoSave);
        return true;
    }

    private bool IsNotificationForToday(DateTime notificationDate)
    {
        var now = dateTimeProvider.UtcNow();
        var endOfDay = now.Date.AddDays(1).AddTicks(-1);
        return notificationDate >= now && notificationDate <= endOfDay;
    }
}