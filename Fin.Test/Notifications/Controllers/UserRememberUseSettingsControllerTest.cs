using Fin.Api.Notifications;
using Fin.Application.Notifications.Services.CrudServices;
using Fin.Domain.Notifications.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Notifications.Controllers;

public class UserRememberUseSettingsControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IUserRememberUseSettingsService> _serviceMock;
    private readonly UserRememberUseSettingsController _controller;

    public UserRememberUseSettingsControllerTest()
    {
        _serviceMock = new Mock<IUserRememberUseSettingsService>();
        _controller = new UserRememberUseSettingsController(_serviceMock.Object);
    }

    [Fact]
    public async Task Get_ShouldReturnSettingsFromService()
    {
        // Arrange
        var expectedOutput = new UserRememberUseSettingOutput
        {
            Id = TestUtils.Guids[0],
            NotifyOn = TestUtils.TimeSpans[3]
        };
        _serviceMock.Setup(s => s.GetByCurrentUser()).ReturnsAsync(expectedOutput);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedOutput);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var input = new UserRememberUseSettingInput
        {
            NotifyOn = TestUtils.TimeSpans[3]
        };
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
        var input = new UserRememberUseSettingInput
        {
            NotifyOn = TestUtils.TimeSpans[3]
        };
        _serviceMock.Setup(s => s.UpdateByCurrentUser(input, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Update(input);

        // Assert
        result.Should().BeOfType<UnprocessableEntityResult>();
    }
}