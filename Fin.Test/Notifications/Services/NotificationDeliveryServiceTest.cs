using Fin.Application.Notifications.Services.DeliveryServices;
using Fin.Domain.Global;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;
using Fin.Domain.Users.Factories;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.Firebases;
using Fin.Infrastructure.Notifications.Hubs;
using FirebaseAdmin.Messaging;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Notification = Fin.Domain.Notifications.Entities.Notification;

namespace Fin.Test.Notifications.Services;

public class NotificationDeliveryServiceTest : TestUtils.BaseTestWithContext
{
    #region SendNotification

    [Fact]
    public async Task SendNotification_ShouldThrow_WhenDeliveryNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var notifyDto = new NotifyUserDto { NotificationId = TestUtils.Guids[1], UserId = TestUtils.Guids[2] };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.SendNotification(notifyDto));
    }

    [Fact]
    public async Task SendNotification_ShouldThrow_WhenSettingsNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();

        var notifyDto = new NotifyUserDto { NotificationId = TestUtils.Guids[1], UserId = AmbientData.UserId.Value };
        var delivery = new NotificationUserDelivery(notifyDto.UserId, notifyDto.NotificationId)
        {
            Notification = new Notification { Id = notifyDto.NotificationId }
        };
        await resources.DeliveryRepository.AddAsync(delivery, true);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.SendNotification(notifyDto));
    }

    [Fact]
    public async Task SendNotification_ShouldSendToHub_ForSnackWay()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();

        var userId = AmbientData.UserId.Value;

        var notifyDto = new NotifyUserDto { NotificationId = TestUtils.Guids[1], UserId = userId, Ways = [NotificationWay.Snack]};
        var delivery = new NotificationUserDelivery(notifyDto.UserId, notifyDto.NotificationId)
        {
            Notification = new Notification { Id = notifyDto.NotificationId }
        };
        var settings = new UserNotificationSettings() { UserId = notifyDto.UserId };
        await resources.DeliveryRepository.AddAsync(delivery, true);
        await resources.UserSettingsRepository.AddAsync(settings, true);

        // Act
        await service.SendNotification(notifyDto);

        // Assert
        resources.FakeClientProxy.Verify(c => c.SendCoreAsync("ReceiveNotification", It.Is<object[]>(o => o[0] == notifyDto), default), Times.Once);
        var dbDelivery = await resources.DeliveryRepository.Query(false).FirstOrDefaultAsync(a => a.NotificationId == delivery.NotificationId);
        dbDelivery.Delivery.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotification_ShouldSendPush_AndRemoveInvalidTokens()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();

        var userId = AmbientData.UserId.Value;

        var token1 = "valid-token";
        var token2 = "invalid-token";

        var notifyDto = new NotifyUserDto
            { NotificationId = TestUtils.Guids[1], UserId = userId, Ways = [NotificationWay.Push] };
        var delivery = new NotificationUserDelivery(userId, notifyDto.NotificationId)
        {
            Notification = new Notification { Id = notifyDto.NotificationId }
        };
        var settings = new UserNotificationSettings() { UserId = notifyDto.UserId, Enabled = true, AllowedWays = [NotificationWay.Push]};
        settings.AddTokenIfNotExist(token1);
        settings.AddTokenIfNotExist(token2);

        await resources.DeliveryRepository.AddAsync(delivery, true);
        await resources.UserSettingsRepository.AddAsync(settings, true);

        resources.FakeFirebaseNotification
            .Setup(f => f.SendPushNotificationAsync(It.IsAny<List<Message>>()))
            .ReturnsAsync([token2]); // Simulate that token2 is invalid and should be removed.

        // Act
        await service.SendNotification(notifyDto);

        // Assert
        resources.FakeFirebaseNotification.Verify(f => f.SendPushNotificationAsync(It.Is<List<Message>>(l => l.Count == 2)), Times.Once);

        var dbSettings = await resources.UserSettingsRepository.Query(false).FirstAsync(s => s.UserId == userId);
        dbSettings.FirebaseTokens.Should().HaveCount(1);
        dbSettings.FirebaseTokens.Should().Contain(token1);
        dbSettings.FirebaseTokens.Should().NotContain(token2);
    }

    [Fact]
    public async Task SendNotification_ShouldSendEmail_WhenAllowed()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();

        var userId = AmbientData.UserId.Value;
        var email = "test@email.com";
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var notifyDto = new NotifyUserDto { NotificationId = TestUtils.Guids[1], UserId = userId, Ways = [NotificationWay.Email], Title = "Email Title", HtmlBody = "<p>Email Body</p>"};
        var delivery = new NotificationUserDelivery(userId, notifyDto.NotificationId)
        {
            Notification = new Notification { Id = notifyDto.NotificationId }
        };
        var settings = new UserNotificationSettings() { UserId = userId, Enabled = true, AllowedWays = [NotificationWay.Email]};
        var credential = UserCredentialFactory.Create(userId, encryptedEmail, null, UserCredentialFactoryType.Password);

        await resources.DeliveryRepository.AddAsync(delivery, true);
        await resources.UserSettingsRepository.AddAsync(settings, true);
        await resources.CredentialRepository.AddAsync(credential, true);

        // Act
        await service.SendNotification(notifyDto);

        // Assert
        resources.FakeEmailSender.Verify(e => e.SendEmailAsync(email, notifyDto.Title, notifyDto.HtmlBody), Times.Once);
    }

    #endregion

    #region MarkAsVisualized

    [Fact]
    public async Task MarkAsVisualized_ShouldThrow_WhenUserNotLogged()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        AmbientData.SetNotLogged();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.MarkAsVisualized(TestUtils.Guids[0]));
    }

    [Fact]
    public async Task MarkAsVisualized_ShouldReturnFalse_WhenDeliveryNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();

        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.MarkAsVisualized(TestUtils.Guids[9]);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsVisualized_ShouldUpdateAndReturnTrue_WhenFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();

        var resources = GetResources();
        var service = GetService(resources);
        var userId = AmbientData.UserId.Value;
        var notificationId = TestUtils.Guids[1];

        var delivery = new NotificationUserDelivery(userId, notificationId)
        {
            Notification = new Notification { Id = notificationId }
        };
        await resources.DeliveryRepository.AddAsync(delivery, true);

        // Act
        var result = await service.MarkAsVisualized(notificationId);

        // Assert
        result.Should().BeTrue();
        var dbDelivery = await resources.DeliveryRepository.Query(false).FirstAsync(s => s.NotificationId == delivery.NotificationId);
        dbDelivery.Visualized.Should().BeTrue();
    }

    #endregion

    #region GetUnvisualizedNotifications

    [Fact]
    public async Task GetUnvisualizedNotifications_ShouldReturnActiveAndMarkVisualized()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();

        var resources = GetResources();
        var service = GetService(resources);
        var userId = AmbientData.UserId.Value;
        var now = TestUtils.UtcDateTimes[5];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);

        // 1. Active & Unvisualized (Should be returned)
        var activeNotification = new Notification { Id = TestUtils.Guids[0], StartToDelivery = now.AddHours(-1), Ways = [NotificationWay.Message]};
        var activeDelivery = new NotificationUserDelivery(userId, activeNotification.Id) { Notification = activeNotification, Visualized = false };

        // 2. Already Visualized
        var visualizedNotification = new Notification { StartToDelivery = now.AddHours(-1), Ways = [NotificationWay.Message] };
        var visualizedDelivery = new NotificationUserDelivery(userId, visualizedNotification.Id) { Notification = visualizedNotification, Visualized = true };

        // 3. Not yet started
        var futureNotification = new Notification { StartToDelivery = now.AddDays(1), Ways = [NotificationWay.Message] };
        var futureDelivery = new NotificationUserDelivery(userId, futureNotification.Id) { Notification = futureNotification, Visualized = false };

        await resources.DeliveryRepository.AddRangeAsync([activeDelivery, visualizedDelivery, futureDelivery], true);

        // Act
        var result = await service.GetUnvisualizedNotifications();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].NotificationId.Should().Be(activeNotification.Id);

        var dbDelivery = await resources.DeliveryRepository.Query(false).FirstAsync(d => d.NotificationId == activeDelivery.NotificationId);
        dbDelivery.Visualized.Should().BeTrue();
    }

    #endregion

    private NotificationDeliveryService GetService(Resources resources)
    {
        return new NotificationDeliveryService(
            resources.DeliveryRepository,
            resources.CredentialRepository,
            resources.UserSettingsRepository,
            resources.FakeConfiguration.Object,
            AmbientData,
            DateTimeProvider.Object,
            resources.FakeHubContext.Object,
            resources.FakeEmailSender.Object,
            resources.FakeFirebaseNotification.Object
        );
    }

    private Resources GetResources()
    {
        var fakeClientProxy = new Mock<IClientProxy>();
        var fakeHubClients = new Mock<IHubClients>();
        fakeHubClients.Setup(c => c.User(It.IsAny<string>())).Returns(fakeClientProxy.Object);
        var fakeHubContext = new Mock<IHubContext<NotificationHub>>();
        fakeHubContext.Setup(h => h.Clients).Returns(fakeHubClients.Object);

        var fakeConfig = new Mock<IConfiguration>();
        fakeConfig.Setup(c => c.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value).Returns("1234567890qwerty1234567890qwerty");
        fakeConfig.Setup(c => c.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value).Returns("1234567890qwerty");

        var encryptKey = fakeConfig.Object.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value!;
        var encryptIv = fakeConfig.Object.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value!;

        return new Resources
        {
            DeliveryRepository = GetRepository<NotificationUserDelivery>(),
            CredentialRepository = GetRepository<UserCredential>(),
            UserSettingsRepository = GetRepository<UserNotificationSettings>(),
            FakeConfiguration = fakeConfig,
            FakeEmailSender = new Mock<IEmailSenderService>(),
            FakeFirebaseNotification = new Mock<IFirebaseNotificationService>(),
            FakeHubContext = fakeHubContext,
            FakeHubClients = fakeHubClients,
            FakeClientProxy = fakeClientProxy,
            CryptoHelper = new CryptoHelper(encryptKey, encryptIv)
        };
    }

    private class Resources
    {
        public IRepository<NotificationUserDelivery> DeliveryRepository { get; set; }
        public IRepository<UserCredential> CredentialRepository { get; set; }
        public IRepository<UserNotificationSettings> UserSettingsRepository { get; set; }
        public Mock<IConfiguration> FakeConfiguration { get; set; }
        public Mock<IEmailSenderService> FakeEmailSender { get; set; }
        public Mock<IFirebaseNotificationService> FakeFirebaseNotification { get; set; }
        public Mock<IHubContext<NotificationHub>> FakeHubContext { get; set; }
        public Mock<IHubClients> FakeHubClients { get; set; }
        public Mock<IClientProxy> FakeClientProxy { get; set; }
        public CryptoHelper CryptoHelper { get; set; }
    }
}