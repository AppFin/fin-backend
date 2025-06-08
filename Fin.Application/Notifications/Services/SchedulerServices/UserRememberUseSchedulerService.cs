using Fin.Application.Notifications.Extensions;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.Services.SchedulerServices;

public interface IUserRememberUseSchedulerService
{
    public Task ScheduleTodayNotification(bool autoSave = true);
}

public class UserRememberUseSchedulerService(
    IRepository<UserRememberUseSetting> rememberRepository,
    IRepository<Notification> notificationRepository,
    IDateTimeProvider dateTimeProvider
    ): IUserRememberUseSchedulerService, IAutoTransient
{
    public async Task ScheduleTodayNotification(bool autoSave = true)
    {
        var startOfDay = dateTimeProvider.UtcNow().Date;
        var dayOfWeek = startOfDay.DayOfWeek;

        var remembers = await rememberRepository.Query(false)
            // Here weekdays is a string of a [] in database, like '[1,2,3,...]'. We neeed to convert this to a string include in database
            .Where(n => n.WeekDays.Contains(dayOfWeek) && n.Ways.Count > 1)
            .ToListAsync();

        foreach (var remember in remembers)
        {
            var notification = remember.ToNotification(startOfDay);
            await notificationRepository.AddAsync(notification);
        }

        if (autoSave) await notificationRepository.SaveChangesAsync();
    }
}