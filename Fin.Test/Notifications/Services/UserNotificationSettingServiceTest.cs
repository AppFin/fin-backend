using Fin.Application.Notifications.Services.CrudServices;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.Notifications.Services;

public class UserNotificationSettingServiceTest : TestUtils.BaseTestWithContext
{
    #region GetByCurrentUser

    [Fact]
    public async Task GetByCurrentUser_ShouldReturnSettings_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();
        var userId = AmbientData.UserId.Value;

        var settings = new UserNotificationSettings() { Enabled = false,  UserId = userId };
        await resources.Repository.AddAsync(settings, false);
        await Context.SaveChangesAsync();

        // Act
        var result = await service.GetByCurrentUser();

        // Assert
        result.Should().NotBeNull();
        result.Enabled.Should().Be(false);
    }

    [Fact]
    public async Task GetByCurrentUser_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.GetByCurrentUser();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateByCurrentUser

    [Fact]
    public async Task UpdateByCurrentUser_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = new UserNotificationSettingsInput();

        // Act
        var result = await service.UpdateByCurrentUser(input, true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateByCurrentUser_ShouldUpdateAndReturnTrue_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();
        var userId = AmbientData.UserId.Value;

        var settings = new UserNotificationSettings() { Enabled = false,  UserId = userId, AllowedWays = [ NotificationWay.Email ]};
        await resources.Repository.AddAsync(settings, false);
        await Context.SaveChangesAsync();

        var input = new UserNotificationSettingsInput { Enabled = true, AllowedWays = [ NotificationWay.Email, NotificationWay.Message ] };

        // Act
        var result = await service.UpdateByCurrentUser(input, true);

        // Assert
        result.Should().BeTrue();

        var dbSettings = await resources.Repository.AsNoTracking().FirstAsync(s => s.UserId == userId);
        dbSettings.Enabled.Should().Be(true);
        dbSettings.AllowedWays.Should().BeEquivalentTo(input.AllowedWays);
    }

    #endregion

    #region AddFirebaseToken

    [Fact]
    public async Task AddFirebaseToken_ShouldReturnFalse_WhenSettingsNotExist()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.AddFirebaseToken(TestUtils.Strings[4], true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddFirebaseToken_ShouldAddToken_WhenTokenIsNew()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();
        var userId = AmbientData.UserId.Value;

        var settings = new UserNotificationSettings() { UserId = userId };
        var newToken = TestUtils.Strings[4];
        await resources.Repository.AddAsync(settings, true);

        await Context.SaveChangesAsync();


        // Act
        var result = await service.AddFirebaseToken(newToken, true);

        // Assert
        result.Should().BeTrue();
        var dbSettings = await resources.Repository.AsNoTracking().FirstAsync(s => s.UserId == userId);
        dbSettings.FirebaseTokens.Should().Contain(newToken);
        dbSettings.FirebaseTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddFirebaseToken_ShouldNotAddDuplicate_WhenTokenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();
        var userId = AmbientData.UserId.Value;

        var existingToken = TestUtils.Strings[4];
        var settings = new UserNotificationSettings() { UserId = userId};
        settings.AddTokenIfNotExist(existingToken);
        await resources.Repository.AddAsync(settings, true);

        await Context.SaveChangesAsync();

        // Act
        var result = await service.AddFirebaseToken(existingToken, true);

        // Assert
        result.Should().BeTrue();
        var dbSettings = await resources.Repository.AsNoTracking().FirstAsync(s => s.UserId == userId);
        dbSettings.FirebaseTokens.Should().Contain(existingToken);
        dbSettings.FirebaseTokens.Should().HaveCount(1);
    }

    #endregion

    private UserNotificationSettingService GetService(Resources resources)
    {
        return new UserNotificationSettingService(resources.Repository, resources.AmbientData);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            Repository = GetRepository<UserNotificationSettings>(),
            AmbientData = this.AmbientData
        };
    }

    private class Resources
    {
        public IRepository<UserNotificationSettings> Repository { get; set; }
        public IAmbientData AmbientData { get; set; }
    }
}