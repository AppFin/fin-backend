using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Fin.Application.Notifications.Services.DeliveryServices;
using Fin.Application.Notifications.Services.SchedulerServices;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.BackgroundJobs;
using Fin.Infrastructure.Database.Repositories;
using Moq;

namespace Fin.Test.Notifications.Services;

public class UserSchedulerServiceTest : TestUtils.BaseTestWithContext
{
    #region ScheduleDailyNotifications

    [Fact]
    public async Task ScheduleDailyNotifications_ShouldSelectOnlyUndeliveredNotificationsForToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var now = TestUtils.UtcDateTimes[0];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);

        var users = new List<User>
        {
            new() { Id = TestUtils.Guids[5] },
            new() { Id = TestUtils.Guids[6] },
            new() { Id = TestUtils.Guids[7] }
        };
        await Context.Users.AddRangeAsync(users);
        await Context.SaveChangesAsync();


        // 1. Target Notification: For today, 1 delivered, 1 undelivered.
        var targetNotification = new Notification { Id = TestUtils.Guids[1], StartToDelivery = now.AddHours(2) };
        targetNotification.UserDeliveries = new Collection<NotificationUserDelivery>
        {
            new(TestUtils.Guids[5], targetNotification.Id) { Delivery = true }, // Delivered
            new(TestUtils.Guids[6], targetNotification.Id) { Delivery = false, BackgroundJobId = TestUtils.Strings[5]} // UNDELIVERED - should be scheduled
        };

        // 2. Wrong Day Notification
        var wrongDayNotification = new Notification { Id = TestUtils.Guids[2], StartToDelivery = now.AddDays(1) };

        // 3. All Delivered Notification
        var allDeliveredNotification = new Notification { Id = TestUtils.Guids[3], StartToDelivery = now.AddHours(3) };
        allDeliveredNotification.UserDeliveries = new Collection<NotificationUserDelivery>
        {
            new(TestUtils.Guids[7], allDeliveredNotification.Id) { Delivery = true }
        };

        await resources.NotificationRepository.AddRangeAsync([targetNotification, wrongDayNotification, allDeliveredNotification], true);

        // Act
        await service.ScheduleDailyNotifications();

        // Assert
        resources.FakeRememberUseSchedulerService.Verify(r => r.ScheduleTodayNotification(true), Times.Once);

        resources.FakeBackgroundJobManager.Verify(b => b.Delete(TestUtils.Strings[5]), Times.Once);
        resources.FakeBackgroundJobManager.Verify(b => b.Schedule(
            It.IsAny<Expression<Action<INotificationDeliveryService>>>(), targetNotification.StartToDelivery), Times.Once);

        resources.FakeBackgroundJobManager.VerifyNoOtherCalls();
    }

    #endregion

    #region ScheduleNotification

    [Fact]
    public void ScheduleNotification_ShouldScheduleJobsForAllUserDeliveries()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var notification = new Notification
        {
            Id = TestUtils.Guids[0],
            StartToDelivery = TestUtils.UtcDateTimes[1]
        };
        notification.UserDeliveries = new Collection<NotificationUserDelivery>
        {
            new(notification.Id, TestUtils.Guids[1]),
            new(notification.Id, TestUtils.Guids[2])
        };

        // Act
        service.ScheduleNotification(notification);

        // Assert
        resources.FakeBackgroundJobManager.Verify(b => b.Schedule( It.IsAny<Expression<Action<INotificationDeliveryService>>>(), notification.StartToDelivery), Times.Exactly(2));
    }

    #endregion

    #region UnscheduleNotification

    [Fact]
    public async Task UnscheduleNotification_ShouldDeleteJobsForUserIds()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var notificationId = TestUtils.Guids[0];
        var userIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[2] };


        await GetRepository<User>().AddRangeAsync([new User { Id = TestUtils.Guids[1] }, new User { Id = TestUtils.Guids[2] }]);
        await resources.NotificationRepository.AddAsync(new Notification { Id = notificationId });
        await resources.NotificationUserDeliveryRepository.AddRangeAsync([
            new NotificationUserDelivery
            {
                NotificationId = notificationId, UserId = TestUtils.Guids[1], BackgroundJobId = TestUtils.Strings[5]
            },
            new NotificationUserDelivery
                { NotificationId = notificationId, UserId = TestUtils.Guids[2], BackgroundJobId = TestUtils.Strings[1] }
        ]);
        await resources.NotificationRepository.SaveChangesAsync();

        // Act
        await service.UnscheduleNotification(notificationId, userIds);

        // Assert

        resources.FakeBackgroundJobManager.Verify(b => b.Delete(TestUtils.Strings[5]), Times.Once);
        resources.FakeBackgroundJobManager.Verify(b => b.Delete(TestUtils.Strings[1]), Times.Once);
        resources.FakeBackgroundJobManager.VerifyNoOtherCalls();
    }

    #endregion

    private UserSchedulerService GetService(Resources resources)
    {
        return new UserSchedulerService(
            resources.NotificationRepository,
            resources.NotificationUserDeliveryRepository,
            DateTimeProvider.Object,
            resources.FakeRememberUseSchedulerService.Object,
            resources.FakeBackgroundJobManager.Object // Inject the new mock
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            NotificationRepository = GetRepository<Notification>(),
            NotificationUserDeliveryRepository = GetRepository<NotificationUserDelivery>(),
            FakeRememberUseSchedulerService = new Mock<IUserRememberUseSchedulerService>(),
            FakeBackgroundJobManager = new Mock<IBackgroundJobManager>()
        };
    }

    private class Resources
    {
        public IRepository<Notification> NotificationRepository { get; set; }
        public IRepository<NotificationUserDelivery> NotificationUserDeliveryRepository { get; set; }
        public Mock<IUserRememberUseSchedulerService> FakeRememberUseSchedulerService { get; set; }
        public Mock<IBackgroundJobManager> FakeBackgroundJobManager { get; set; } // Add to resources
    }
}