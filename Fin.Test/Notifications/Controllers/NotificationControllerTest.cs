using Fin.Api.Notifications;
using Fin.Application.Notifications.Services.CrudServices;
using Fin.Application.Notifications.Services.DeliveryServices;
using Fin.Domain.Global.Classes;
using Fin.Domain.Notifications.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Notifications.Controllers;

public class NotificationControllerTest : TestUtils.BaseTest
{
    private readonly Mock<INotificationService> _serviceMock;
    private readonly Mock<INotificationDeliveryService> _deliveryServiceMock;
    private readonly NotificationController _controller;

    public NotificationControllerTest()
    {
        _serviceMock = new Mock<INotificationService>();
        _deliveryServiceMock = new Mock<INotificationDeliveryService>();
        _controller = new NotificationController(_serviceMock.Object, _deliveryServiceMock.Object);
    }

    [Fact]
    public async Task GetList_ShouldReturnPagedOutput()
    {
        // Arrange
        var input = new PagedFilteredAndSortedInput();
        var expectedOutput = new PagedOutput<NotificationOutput>(1,
        [
            new NotificationOutput { Id = TestUtils.Guids[0], Title = TestUtils.Strings[1] }
        ]);

        _serviceMock.Setup(s => s.GetList(input)).ReturnsAsync(expectedOutput);

        // Act
        var result = await _controller.GetList(input);

        // Assert
        result.Should().BeEquivalentTo(expectedOutput);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenNotificationExists()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        var expectedNotification = new NotificationOutput { Id = notificationId, Title = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Get(notificationId)).ReturnsAsync(expectedNotification);

        // Act
        var result = await _controller.Get(notificationId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedNotification);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Get(notificationId)).ReturnsAsync((NotificationOutput)null);

        // Act
        var result = await _controller.Get(notificationId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new NotificationInput { Title = TestUtils.Strings[1] };
        var createdNotification = new NotificationOutput { Id = TestUtils.Guids[0], Title = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync(createdNotification);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdNotification);

        (result.Result as CreatedResult)?.Location.Should().Be($"notifications/{createdNotification.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenCreationFails()
    {
        // Arrange
        var input = new NotificationInput();
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync((NotificationOutput)null);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<UnprocessableEntityResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        var input = new NotificationInput { Title = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Update(notificationId, input, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(notificationId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenUpdateFails()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        var input = new NotificationInput();
        _serviceMock.Setup(s => s.Update(notificationId, input, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Update(notificationId, input);

        // Assert
        result.Should().BeOfType<UnprocessableEntityResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Delete(notificationId, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(notificationId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnUnprocessableEntity_WhenDeleteFails()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Delete(notificationId, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(notificationId);

        // Assert
        result.Should().BeOfType<UnprocessableEntityResult>();
    }

    [Fact]
    public async Task MarkVisualized_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        _deliveryServiceMock.Setup(s => s.MarkAsVisualized(notificationId, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.MarkVisualized(notificationId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task MarkVisualized_ShouldReturnNotFound_WhenFails()
    {
        // Arrange
        var notificationId = TestUtils.Guids[0];
        _deliveryServiceMock.Setup(s => s.MarkAsVisualized(notificationId, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.MarkVisualized(notificationId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUnvisualizedNotification_ShouldReturnListOfNotifications()
    {
        // Arrange
        var expectedList = new List<NotifyUserDto>
        {
            new() { NotificationId = TestUtils.Guids[0], Title = TestUtils.Strings[1] },
            new() { NotificationId = TestUtils.Guids[1], Title = TestUtils.Strings[2] }
        };
        _deliveryServiceMock.Setup(s => s.GetUnvisualizedNotifications(true)).ReturnsAsync(expectedList);

        // Act
        var result = await _controller.GetUnvisualizedNotification();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedList);
    }
}