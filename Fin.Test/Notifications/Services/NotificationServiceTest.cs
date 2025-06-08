using Fin.Application.Notifications.Services.CrudServices;
using Fin.Application.Notifications.Services.SchedulerServices;
using Fin.Domain.Global.Classes;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fin.Test.Notifications.Services;

public class NotificationServiceTest : TestUtils.BaseTestWithContext
{
    #region Get

    [Fact]
    public async Task Get_ShouldReturnNotification_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var notification = new Notification(new NotificationInput { Title = TestUtils.Strings[0] });
        await resources.NotificationRepository.AddAsync(notification, true);

        // Act
        var result = await service.Get(notification.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(notification.Id);
        result.Title.Should().Be(notification.Title);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Get(TestUtils.Guids[9]);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await resources.NotificationRepository.AddAsync(new Notification(new NotificationInput { Title = "A" }), true);
        await resources.NotificationRepository.AddAsync(new Notification(new NotificationInput { Title = "B" }), true);

        var input = new PagedFilteredAndSortedInput { MaxResultCount = 1, SkipCount = 1 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("B");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldSchedule_WhenDateIsForToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new NotificationInput
        {
            Title = "Test",
            StartToDelivery = TestUtils.UtcDateTimes[0].AddHours(1) // Date is for "today"
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        var dbNotification = await resources.NotificationRepository.Query(false).FirstOrDefaultAsync(a => a.Id == result.Id);
        dbNotification.Should().NotBeNull();

        resources.FakeSchedulerService
            .Verify(s => s.ScheduleNotification(It.Is<Notification>(n => n.Id == result.Id)), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldNotSchedule_WhenDateIsNotForToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new NotificationInput
        {
            Title = "Test",
            StartToDelivery = TestUtils.UtcDateTimes[0].AddDays(2) // Date is NOT for "today"
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        var dbNotification = await resources.NotificationRepository.Query(false).FirstOrDefaultAsync(a => a.Id == result.Id);
        dbNotification.Should().NotBeNull();

        resources.FakeSchedulerService
            .Verify(s => s.ScheduleNotification(It.IsAny<Notification>()), Times.Never);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnFalse_WhenNotificationNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Update(TestUtils.Guids[9], new NotificationInput(), true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldUnschedule_WhenDateChangesFromTodayToNotToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var notification = new Notification(new NotificationInput { StartToDelivery = TestUtils.UtcDateTimes[0].AddHours(2) });
        await resources.NotificationRepository.AddAsync(notification, true);

        var input = new NotificationInput { StartToDelivery = TestUtils.UtcDateTimes[0].AddDays(3) };

        // Act
        var result = await service.Update(notification.Id, input, true);

        // Assert
        result.Should().BeTrue();
        resources.FakeSchedulerService
            .Verify(s => s.UnscheduleNotification(notification.Id, It.IsAny<List<Guid>>()), Times.Once);
        resources.FakeSchedulerService
            .Verify(s => s.ScheduleNotification(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldSchedule_WhenDateChangesFromNotTodayToToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var notification = new Notification(new NotificationInput { StartToDelivery = TestUtils.UtcDateTimes[0].AddDays(2) });
        await resources.NotificationRepository.AddAsync(notification, true);

        var input = new NotificationInput { StartToDelivery = TestUtils.UtcDateTimes[0].AddHours(3) };

        // Act
        var result = await service.Update(notification.Id, input, true);

        // Assert
        result.Should().BeTrue();
        resources.FakeSchedulerService
            .Verify(s => s.UnscheduleNotification(It.IsAny<Guid>(), It.IsAny<List<Guid>>()), Times.Never);
        resources.FakeSchedulerService
            .Verify(s => s.ScheduleNotification(It.Is<Notification>(n => n.Id == notification.Id)), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReschedule_WhenDateStaysForToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var user = new List<User>
        {
            new() { Id = TestUtils.Guids[1] },
            new() { Id = TestUtils.Guids[2] },
            new() { Id = TestUtils.Guids[3] }
        };
        await resources.UsersRepository.AddRangeAsync(user);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var notification = new Notification(new NotificationInput
        {
            StartToDelivery = TestUtils.UtcDateTimes[0].AddHours(2),
            UserIds = [TestUtils.Guids[1], TestUtils.Guids[2]]
        });
        await resources.NotificationRepository.AddAsync(notification);
        await UnitOfWork.SaveChangesAsync();

        var input = new NotificationInput
        {
            StartToDelivery = TestUtils.UtcDateTimes[0].AddHours(4),
            UserIds = [TestUtils.Guids[1], TestUtils.Guids[3]] // User 2 removed, User 3 added
        };

        // Act
        var result = await service.Update(notification.Id, input, true);

        // Assert
        result.Should().BeTrue();
        // Unschedule should be called only for the removed user
        resources.FakeSchedulerService
            .Verify(s => s.UnscheduleNotification(notification.Id, It.Is<List<Guid>>(l => l.Count == 1 && l.Contains(TestUtils.Guids[2]))), Times.Once);
        // Schedule should be called for the whole updated notification
        resources.FakeSchedulerService
            .Verify(s => s.ScheduleNotification(It.Is<Notification>(n => n.Id == notification.Id)), Times.Once);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnFalse_WhenNotificationNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Delete(TestUtils.Guids[9], true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldUnschedule_WhenDateIsForToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var notification = new Notification(new NotificationInput { StartToDelivery = TestUtils.UtcDateTimes[0].AddHours(1) });
        await resources.NotificationRepository.AddAsync(notification, true);

        // Act
        var result = await service.Delete(notification.Id, true);

        // Assert
        result.Should().BeTrue();
        (await resources.NotificationRepository.Query(false).FirstOrDefaultAsync(a => a.Id == notification.Id)).Should().BeNull();
        resources.FakeSchedulerService
            .Verify(s => s.UnscheduleNotification(notification.Id, It.IsAny<List<Guid>>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldNotUnschedule_WhenDateIsNotForToday()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var notification = new Notification(new NotificationInput { StartToDelivery = TestUtils.UtcDateTimes[0].AddDays(1) });
        await resources.NotificationRepository.AddAsync(notification, true);

        // Act
        var result = await service.Delete(notification.Id, true);

        // Assert
        result.Should().BeTrue();
        (await resources.NotificationRepository.Query(false).FirstOrDefaultAsync(a => a.Id == notification.Id)).Should().BeNull();
        resources.FakeSchedulerService
            .Verify(s => s.UnscheduleNotification(It.IsAny<Guid>(), It.IsAny<List<Guid>>()), Times.Never);
    }

    #endregion

    private NotificationService GetService(Resources resources)
    {
        return new NotificationService(
            resources.NotificationRepository,
            resources.DeliveriesRepository,
            DateTimeProvider.Object,
            resources.FakeSchedulerService.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            NotificationRepository = GetRepository<Notification>(),
            DeliveriesRepository = GetRepository<NotificationUserDelivery>(),
            UsersRepository = GetRepository<User>(),
            FakeSchedulerService = new Mock<IUserSchedulerService>()
        };
    }

    private class Resources
    {
        public IRepository<Notification> NotificationRepository { get; set; }
        public IRepository<NotificationUserDelivery> DeliveriesRepository { get; set; }
        public IRepository<User> UsersRepository { get; set; }
        public Mock<IUserSchedulerService> FakeSchedulerService { get; set; }
    }
}