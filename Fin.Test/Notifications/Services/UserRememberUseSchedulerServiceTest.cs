using Fin.Application.Notifications.Services.SchedulerServices;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.Notifications.Services;

public class UserRememberUseSchedulerServiceTest : TestUtils.BaseTestWithContext
{
    #region ScheduleTodayNotification

    [Fact]
    public async Task ScheduleTodayNotification_ShouldCreateNotifications_OnlyForMatchingSettings()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var today = TestUtils.UtcDateTimes[1];
        var todayDayOfWeek = today.DayOfWeek;
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(today);

        var users = new List<User>
        {
            new() { Id = TestUtils.Guids[1] },
            new() { Id = TestUtils.Guids[2] },
            new() { Id = TestUtils.Guids[3] }
        };
        await Context.Users.AddRangeAsync(users);
        await Context.SaveChangesAsync();

        // 1. Valid Setting: Should be processed
        var user1Setting = new UserRememberUseSetting()
        {
            UserId = users[0].Id,
            WeekDays = [todayDayOfWeek, (DayOfWeek)((int)todayDayOfWeek + 1)],
            Ways = [NotificationWay.Push, NotificationWay.Snack]
        };

        // 2. Invalid Setting: Day of week does not match
        var user2Setting = new UserRememberUseSetting()
        {
            UserId = users[1].Id,
            WeekDays = [(DayOfWeek)((int)todayDayOfWeek + 1)], // Does not include todayDayOfWeek
            Ways = [NotificationWay.Push, NotificationWay.Email]
        };

        // 3. Valid Setting: Should also be processed
        var user3Setting = new UserRememberUseSetting()
        {
            UserId = users[2].Id,
            WeekDays = [todayDayOfWeek],
            Ways = [NotificationWay.Email]
        };

        await resources.RememberRepository.AddRangeAsync([user1Setting, user2Setting, user3Setting], true);

        // Act
        await service.ScheduleTodayNotification(true);

        // Assert
        var createdNotifications = await resources.NotificationRepository.Query(false).Include(e => e.UserDeliveries).ToListAsync();

        // Only settings for User 1 and User 3 should have resulted in a new notification.
        createdNotifications.Should().HaveCount(2);

        createdNotifications.Should().Contain(n => n.UserDeliveries.Select(a => a.UserId).Contains(user1Setting.UserId));
        createdNotifications.Should().Contain(n => n.UserDeliveries.Select(a => a.UserId).Contains(user3Setting.UserId));

        createdNotifications.Should().NotContain(n => n.UserDeliveries.Select(a => a.UserId).Contains(user2Setting.UserId));
    }

    #endregion

    private UserRememberUseSchedulerService GetService(Resources resources)
    {
        return new UserRememberUseSchedulerService(
            resources.RememberRepository,
            resources.NotificationRepository,
            DateTimeProvider.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            RememberRepository = GetRepository<UserRememberUseSetting>(),
            NotificationRepository = GetRepository<Notification>()
        };
    }

    private class Resources
    {
        public IRepository<UserRememberUseSetting> RememberRepository { get; set; }
        public IRepository<Notification> NotificationRepository { get; set; }
    }
}