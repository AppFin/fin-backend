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

        // Let's set "today" to be a Wednesday.
        var today = new DateTime(2025, 06, 11, 10, 0, 0, DateTimeKind.Utc);
        DayOfWeek todayDayOfWeek = today.DayOfWeek; // Wednesday
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(today);

        var users = new List<User>
        {
            new() { Id = TestUtils.Guids[1] },
            new() { Id = TestUtils.Guids[2] },
            new() { Id = TestUtils.Guids[3] },
            new() { Id = TestUtils.Guids[4] }
        };
        await Context.Users.AddRangeAsync(users);
        await Context.SaveChangesAsync();

        // 1. Valid Setting: Should be processed
        var user1Setting = new UserRememberUseSetting()
        {
            UserId = TestUtils.Guids[1],
            WeekDays = [todayDayOfWeek, DayOfWeek.Friday],
            Ways = [NotificationWay.Push, NotificationWay.Snack] // Has more than 1 way
        };

        // 2. Invalid Setting: Day of week does not match
        var user2Setting = new UserRememberUseSetting()
        {
            UserId = TestUtils.Guids[2],
            WeekDays = [DayOfWeek.Monday, DayOfWeek.Tuesday], // Does not include Wednesday
            Ways = [NotificationWay.Push, NotificationWay.Email]
        };

        // 3. Invalid Setting: Not enough ways
        var user3Setting = new UserRememberUseSetting()
        {
            UserId = TestUtils.Guids[3],
            WeekDays = [todayDayOfWeek],
            Ways = [NotificationWay.Email] // Has only 1 way
        };

        // 4. Valid Setting: Should also be processed
        var user4Setting = new UserRememberUseSetting()
        {
            UserId = TestUtils.Guids[4],
            WeekDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Sunday],
            Ways = [NotificationWay.Message, NotificationWay.Email, NotificationWay.Push] // Has more than 1 way
        };

        await resources.RememberRepository.AddRangeAsync([user1Setting, user2Setting, user3Setting, user4Setting], true);

        // Act
        await service.ScheduleTodayNotification(true);

        // Assert
        var createdNotifications = await resources.NotificationRepository.Query(false).ToListAsync();

        // Only settings for User 1 and User 4 should have resulted in a new notification.
        createdNotifications.Should().HaveCount(2);

        createdNotifications.Should().Contain(n => n.UserDeliveries.Select(a => a.UserId).Contains(user1Setting.UserId));
        createdNotifications.Should().Contain(n => n.UserDeliveries.Select(a => a.UserId).Contains(user4Setting.UserId));

        createdNotifications.Should().NotContain(n => n.UserDeliveries.Select(a => a.UserId).Contains(user2Setting.UserId));
        createdNotifications.Should().NotContain(n => n.UserDeliveries.Select(a => a.UserId).Contains(user3Setting.UserId));
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