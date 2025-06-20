using System.Security;
using Fin.Application.Users.Services;
using Fin.Domain.Global;
using Fin.Domain.Global.Classes;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications.Consts;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.EmailSenders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fin.Test.Users;

public class UserDeleteServiceTest : TestUtils.BaseTestWithContext
{
    #region RequestDeleteUser

    [Fact]
    public async Task RequestDeleteUser_AlreadyRequest_ReturnsFalse()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(false);
        var resources = GetResources();
        var service = GetService(resources);

        var existingRequest = new UserDeleteRequest(AmbientData.UserId.Value, TestUtils.UtcDateTimes[0]);
        await resources.UserDeleteRequestRepository.AddAsync(existingRequest, true);

        // Act
        var result = await service.RequestDeleteUser();

        // Assert
        result.Should().BeFalse();

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestDeleteUser_UserNotFound_ReturnsFalse()
    {
        // Arrange
        AmbientData.SetData(TestUtils.Guids[1], TestUtils.Guids[2], TestUtils.Strings[0], false);
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.RequestDeleteUser();

        // Assert
        result.Should().BeFalse();

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestDeleteUser_Success()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(false);
        var resources = GetResources();
        var service = GetService(resources);

        var now = TestUtils.UtcDateTimes[0];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);

        var user = await resources.UserRepository.Query(false).FirstOrDefaultAsync();
        var email = TestUtils.Strings[2];
        var  encryptedEmail = resources.CryptoHelper.Encrypt(email);
        user.Credential = new UserCredential(user.Id, encryptedEmail, null);
        await resources.CredentialRepository.AddAsync(user.Credential, true);

        // Act
        var result = await service.RequestDeleteUser();

        // Assert
        result.Should().BeTrue();

        var updatedUser = await resources.UserRepository.Query().FirstAsync(u => u.Id == AmbientData.UserId.Value);
        updatedUser.DeleteRequests.First().DeleteRequestedAt.Should().Be(now);
        updatedUser.IsActivity.Should().BeFalse();

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(email, "Solicitação de deleção",
                It.Is<string>(body => body.Contains("Recebemos sua solicitação"))), Times.Once);
    }

    #endregion

    #region EffectiveDeleteUsers

    [Fact]
    public async Task EffectiveDeleteUsers_NoUsersToDelete_ReturnsTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var today = DateOnly.FromDateTime(TestUtils.UtcDateTimes[0]);
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        // Act
        var result = await service.EffectiveDeleteUsers();

        // Assert
        result.Should().BeTrue();
        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);
    }

    [Fact]
    public async Task EffectiveDeleteUsers_WithUsersToDelete_Success()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(false);
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var user = await resources.UserRepository.Query().Include(u => u.Credential).FirstAsync();

        var email = TestUtils.Strings[2];
        var  encryptedEmail = resources.CryptoHelper.Encrypt(email);
        user.Credential = new UserCredential(user.Id, encryptedEmail, null);
        await resources.CredentialRepository.AddAsync(user.Credential, true);

        var deleteRequest = new UserDeleteRequest(user.Id, TestUtils.UtcDateTimes[0]);
        deleteRequest.DeleteEffectivatedAt = DateOnly.FromDateTime(TestUtils.UtcDateTimes[0]);
        await resources.UserDeleteRequestRepository.AddAsync(deleteRequest, true);

        var notificationSetting = new UserNotificationSettings(user.Id, AmbientData.TenantId.Value);
        await resources.NotificationSettingsRepository.AddAsync(notificationSetting, true);

        var rememberSetting = new UserRememberUseSetting(user.Id, AmbientData.TenantId.Value);
        await resources.RememberRepository.AddAsync(rememberSetting, true);

        // Act
        var result = await service.EffectiveDeleteUsers();

        // Assert
        result.Should().BeTrue();

        var deletedUser = await resources.UserRepository.Query().FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser.Should().BeNull();

        var deletedCredential = await resources.CredentialRepository.Query().FirstOrDefaultAsync(c => c.UserId == user.Id);
        deletedCredential.Should().BeNull();

        var deletedRequest = await resources.UserDeleteRequestRepository.Query().FirstOrDefaultAsync(r => r.UserId == user.Id);
        deletedRequest.Should().BeNull();

        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(email, "Conta deletada",
                It.Is<string>(body => body.Contains("foi deletada"))), Times.Once);
    }

    #endregion

    #region AbortDeleteUser

    [Fact]
    public async Task AbortDeleteUser_NotAdminAndNotOwner_ThrowsSecurityException()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(false);
        var resources = GetResources();
        var service = GetService(resources);

        var otherUserId = TestUtils.Guids[3];

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
            service.AbortDeleteUser(otherUserId));
        exception.Message.Should().Be("You are not admin");

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AbortDeleteUser_NoAmbientUserId_ThrowsSecurityException()
    {
        // Arrange
        AmbientData.SetData(TestUtils.Guids[1], TestUtils.Guids[2], TestUtils.Strings[0], false);
        var resources = GetResources();
        var service = GetService(resources);

        var userId = TestUtils.Guids[3];

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
            service.AbortDeleteUser(userId));
        exception.Message.Should().Be("You are not admin");

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Never);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AbortDeleteUser_DeleteRequestNotFound_ReturnsFalse()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(true);
        var resources = GetResources();
        var service = GetService(resources);

        var userId = TestUtils.Guids[3];

        // Act
        var result = await service.AbortDeleteUser(userId);

        // Assert
        result.Should().BeFalse();

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AbortDeleteUser_AsAdmin_Success()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(true);
        var resources = GetResources();
        var service = GetService(resources);

        var email = TestUtils.Strings[2];
        var encryptedEmail = resources.CryptoHelper.Encrypt(email);

        var targetUser = new User { Id = TestUtils.Guids[3] };
        var targetCredential = new UserCredential(targetUser.Id, encryptedEmail, null);
        targetUser.Credential = targetCredential;

        await resources.UserRepository.AddAsync(targetUser, true);

        var deleteRequest = new UserDeleteRequest(targetUser.Id, TestUtils.UtcDateTimes[0]);
        await resources.UserDeleteRequestRepository.AddAsync(deleteRequest, true);

        var now = TestUtils.UtcDateTimes[1];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);

        // Act
        var result = await service.AbortDeleteUser(targetUser.Id);

        // Assert
        result.Should().BeTrue();

        var updatedRequest = await resources.UserDeleteRequestRepository.Query()
            .FirstAsync(r => r.Id == deleteRequest.Id);
        updatedRequest.Aborted.Should().BeTrue();
        updatedRequest.AbortedAt.Should().Be(now);
        updatedRequest.UserAbortedId.Should().Be(AmbientData.UserId.Value);

        DateTimeProvider.Verify(d => d.UtcNow(), Times.Once);
        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(email, "Solicitação de deleção abortada",
                It.Is<string>(body => body.Contains("foi abortada"))), Times.Once);
    }

    [Fact]
    public async Task AbortDeleteUser_AsOwner_Success()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(false);
        var resources = GetResources();
        var service = GetService(resources);

        var user = await resources.UserRepository.Query().Include(u => u.Credential).FirstAsync();
        await resources.CredentialRepository.DeleteAsync(user.Credential, true);

        var email = TestUtils.Strings[2];
        var  encryptedEmail = resources.CryptoHelper.Encrypt(email);
        user.Credential = new UserCredential(user.Id, encryptedEmail, null);
        await resources.CredentialRepository.AddAsync(user.Credential, true);

        var deleteRequest = new UserDeleteRequest(user.Id, TestUtils.UtcDateTimes[0]);
        await resources.UserDeleteRequestRepository.AddAsync(deleteRequest, true);

        var now = TestUtils.UtcDateTimes[1];
        DateTimeProvider.Setup(d => d.UtcNow()).Returns(now);

        // Act
        var result = await service.AbortDeleteUser(user.Id);

        // Assert
        result.Should().BeTrue();

        var updatedRequest = await resources.UserDeleteRequestRepository.Query()
            .FirstAsync(r => r.Id == deleteRequest.Id);
        updatedRequest.Aborted.Should().BeTrue();
        updatedRequest.AbortedAt.Should().Be(now);
        updatedRequest.UserAbortedId.Should().Be(AmbientData.UserId.Value);

        resources.FakeEmailSender
            .Verify(e => e.SendEmailAsync(email, "Solicitação de deleção abortada",
                It.Is<string>(body => body.Contains("foi abortada"))), Times.Once);
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_EmptyList_ReturnsEmptyPagedResult()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new PagedFilteredAndSortedInput
        {
            SkipCount = 0,
            MaxResultCount = 10
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetList_WithData_ReturnsPagedResult()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(false);
        var resources = GetResources();
        var service = GetService(resources);

        var user1 = await resources.UserRepository.Query().FirstAsync();
        var user2 = new User { Id = TestUtils.Guids[3], DisplayName = TestUtils.Strings[1], Credential = new UserCredential()};
        await resources.UserRepository.AddAsync(user2, true);

        var adminUser = new User { Id = TestUtils.Guids[4], DisplayName = TestUtils.Strings[3] };
        adminUser.MakeAdmin();
        await resources.UserRepository.AddAsync(adminUser, true);

        var request1 = new UserDeleteRequest(user1.Id, TestUtils.UtcDateTimes[0]);
        var request2 = new UserDeleteRequest(user2.Id, TestUtils.UtcDateTimes[1]);

        await resources.UserDeleteRequestRepository.AddAsync(request1, true);
        await resources.UserDeleteRequestRepository.AddAsync(request2, true);
        request2.Abort(adminUser.Id, TestUtils.UtcDateTimes[2]);
        await resources.UserDeleteRequestRepository.UpdateAsync(request2, true);

        var input = new PagedFilteredAndSortedInput
        {
            SkipCount = 0,
            MaxResultCount = 10
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        var firstItem = result.Items.First();
        firstItem.UserId.Should().Be(user1.Id);
        firstItem.Aborted.Should().BeFalse();

        var secondItem = result.Items.Last();
        secondItem.UserId.Should().Be(user2.Id);
        secondItem.Aborted.Should().BeTrue();
        secondItem.UserAbortedId.Should().Be(adminUser.Id);
    }

    #endregion

    private UserDeleteService GetService(Resources resources)
    {
        return new UserDeleteService(
            resources.UserDeleteRequestRepository,
            resources.UserRepository,
            DateTimeProvider.Object,
            AmbientData,
            UnitOfWork,
            resources.FakeEmailSender.Object,
            resources.FakeConfiguration.Object,
            resources.CredentialRepository,
            resources.NotificationRepository,
            resources.NotificationDeliveryRepository,
            resources.RememberRepository,
            resources.NotificationSettingsRepository,
            resources.TenantRepository,
            resources.TenantUserRepository
        );
    }

    private Resources GetResources()
    {
        var resources = new Resources
        {
            UserDeleteRequestRepository = GetRepository<UserDeleteRequest>(),
            UserRepository = GetRepository<User>(),
            CredentialRepository = GetRepository<UserCredential>(),
            NotificationRepository = GetRepository<Notification>(),
            NotificationDeliveryRepository = GetRepository<NotificationUserDelivery>(),
            RememberRepository = GetRepository<UserRememberUseSetting>(),
            NotificationSettingsRepository = GetRepository<UserNotificationSettings>(),
            TenantRepository = GetRepository<Tenant>(),
            TenantUserRepository = GetRepository<TenantUser>(),
            FakeEmailSender = new Mock<IEmailSenderService>(),
            FakeConfiguration = new Mock<IConfiguration>(),
        };

        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value)
            .Returns("1234567890qwerty1234567890qwerty");
        resources.FakeConfiguration
            .Setup(c => c.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value)
            .Returns("1234567890qwerty");

        var encryptKey = resources.FakeConfiguration.Object.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value ?? "";
        var encryptIv = resources.FakeConfiguration.Object.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value ?? "";

        resources.CryptoHelper = new CryptoHelper(encryptKey, encryptIv);

        return resources;
    }

    private class Resources
    {
        public IRepository<UserDeleteRequest> UserDeleteRequestRepository { get; init; }
        public IRepository<User> UserRepository { get; init; }
        public IRepository<UserCredential> CredentialRepository { get; init; }
        public IRepository<Notification> NotificationRepository { get; init; }
        public IRepository<NotificationUserDelivery> NotificationDeliveryRepository { get; init; }
        public IRepository<UserRememberUseSetting> RememberRepository { get; init; }
        public IRepository<UserNotificationSettings> NotificationSettingsRepository { get; init; }
        public IRepository<Tenant> TenantRepository { get; init; }
        public IRepository<TenantUser> TenantUserRepository { get; init; }
        public Mock<IEmailSenderService> FakeEmailSender { get; init; }
        public Mock<IConfiguration> FakeConfiguration { get; init; }
        public CryptoHelper CryptoHelper { get; set; }
    }
}