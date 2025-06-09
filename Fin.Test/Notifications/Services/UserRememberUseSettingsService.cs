using Fin.Application.Notifications.Services.CrudServices;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.Notifications.Services;

public class UserRememberUseSettingsServiceTest : TestUtils.BaseTestWithContext
{
    #region GetByCurrentUser

    [Fact]
    public async Task GetByCurrentUser_ShouldReturnSettings_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await ConfigureLoggedAmbientAsync();

        var currentUserId = resources.AmbientData.UserId.Value;
        var settings = new UserRememberUseSetting()
        {
            UserId = currentUserId,
            NotifyOn = TestUtils.TimeSpans[0]
        };
        await resources.Repository.AddAsync(settings, true);

        // Act
        var result = await service.GetByCurrentUser();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(settings.Id);
        result.NotifyOn.Should().Be(settings.NotifyOn);
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
        var input = new UserRememberUseSettingInput();

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
        var currentUserId = resources.AmbientData.UserId.Value;
        var settings = new UserRememberUseSetting() { UserId = currentUserId, NotifyOn = TestUtils.TimeSpans[0] };
        await resources.Repository.AddAsync(settings, true);

        var input = new UserRememberUseSettingInput { NotifyOn = TestUtils.TimeSpans[1] };

        // Act
        var result = await service.UpdateByCurrentUser(input, true);

        // Assert
        result.Should().BeTrue();

        var dbSettings = await resources.Repository.Query(false).FirstAsync(s => s.UserId == currentUserId);
        dbSettings.NotifyOn.Should().Be(input.NotifyOn);
    }

    #endregion

    private UserRememberUseSettingsService GetService(Resources resources)
    {
        return new UserRememberUseSettingsService(resources.Repository, resources.AmbientData);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            Repository = GetRepository<UserRememberUseSetting>(),
            AmbientData = this.AmbientData
        };
    }

    private class Resources
    {
        public IRepository<UserRememberUseSetting> Repository { get; set; }
        public IAmbientData AmbientData { get; set; }
    }
}