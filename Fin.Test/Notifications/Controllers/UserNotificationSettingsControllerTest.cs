using Fin.Api.Notifications;
using Fin.Application.Notifications.Services.CrudServices;
using Fin.Domain.Notifications.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Notifications.Controllers;

public class UserNotificationSettingsControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IUserNotificationSettingService> _serviceMock;
    private readonly UserNotificationSettingsController _controller;

    public UserNotificationSettingsControllerTest()
    {
        _serviceMock = new Mock<IUserNotificationSettingService>();
        _controller = new UserNotificationSettingsController(_serviceMock.Object);
    }

    [Fact]
    public async Task Get_ShouldReturnSettingsFromService()
    {
        // Arrange
        var expectedSettings = new UserNotificationSettingsOutput
        {
            Enabled = true
        };
        _serviceMock.Setup(s => s.GetByCurrentUser()).ReturnsAsync(expectedSettings);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedSettings);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var input = new UserNotificationSettingsInput { Enabled = true };
        _serviceMock.Setup(s => s.UpdateByCurrentUser(input, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenUpdateFails()
    {
        // Arrange
        var input = new UserNotificationSettingsInput { Enabled = true };
        _serviceMock.Setup(s => s.UpdateByCurrentUser(input, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Update(input);

        // Assert
        result.Should().BeOfType<UnprocessableEntityResult>();
    }

    [Fact]
    public async Task AddFirebaseToken_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var fakeToken = TestUtils.Strings[4];
        _serviceMock.Setup(s => s.AddFirebaseToken(fakeToken, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.AddFirebaseToken(fakeToken);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task AddFirebaseToken_ShouldReturnUnprocessableEntity_WhenFails()
    {
        // Arrange
        var fakeToken = TestUtils.Strings[4];
        _serviceMock.Setup(s => s.AddFirebaseToken(fakeToken, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.AddFirebaseToken(fakeToken);

        // Assert
        result.Should().BeOfType<UnprocessableEntityResult>();
    }
}