using Fin.Api.Users;
using Fin.Application.Users.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Users.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Users;

public class UserDeleteControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IUserDeleteService> _serviceMock;
    private readonly UserDeleteController _controller;

    public UserDeleteControllerTest()
    {
        _serviceMock = new Mock<IUserDeleteService>();
        _controller = new UserDeleteController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetList_ShouldReturnPagedOutput_WhenSuccess()
    {
        // Arrange
        var input = new PagedFilteredAndSortedInput
        {
            SkipCount = 0,
            MaxResultCount = 10,
        };

        var deleteRequests = new List<UserDeleteRequestDto>
        {
            new() { Id = TestUtils.Guids[0], UserId = TestUtils.Guids[1] },
            new() { Id = TestUtils.Guids[2], UserId = TestUtils.Guids[3] }
        };

        var pagedOutput = new PagedOutput<UserDeleteRequestDto>
        {
            Items = deleteRequests,
            TotalCount = 2,
        };

        _serviceMock.Setup(s => s.GetList(input, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(pagedOutput);

        // Act
        var result = await _controller.GetList(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().BeEquivalentTo(deleteRequests);
    }

    [Fact]
    public async Task GetList_ShouldCallService_WithCorrectParameters()
    {
        // Arrange
        var input = new PagedFilteredAndSortedInput();
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.GetList(It.IsAny<PagedFilteredAndSortedInput>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new PagedOutput<UserDeleteRequestDto>());

        // Act
        await _controller.GetList(input, cancellationToken);

        // Assert
        _serviceMock.Verify(s => s.GetList(input, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task AbortDelete_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var userId = TestUtils.Guids[0];
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.AbortDeleteUser(userId, cancellationToken))
                   .ReturnsAsync(true);

        // Act
        var response = await _controller.AbortDelete(userId, cancellationToken);

        // Assert
        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().Be(true);
    }

    [Fact]
    public async Task AbortDelete_ShouldReturnNotFound_WhenFail()
    {
        // Arrange
        var userId = TestUtils.Guids[0];
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.AbortDeleteUser(userId, cancellationToken))
                   .ReturnsAsync(false);

        // Act
        var response = await _controller.AbortDelete(userId, cancellationToken);

        // Assert
        var notFound = response.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.Value.Should().Be("Delete request not found or already processed.");
    }

    [Fact]
    public async Task AbortDelete_ShouldCallService_WithCorrectParameters()
    {
        // Arrange
        var userId = TestUtils.Guids[0];
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.AbortDeleteUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        // Act
        await _controller.AbortDelete(userId, cancellationToken);

        // Assert
        _serviceMock.Verify(s => s.AbortDeleteUser(userId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task RequestDelete_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.RequestDeleteUser(cancellationToken))
                   .ReturnsAsync(true);

        // Act
        var response = await _controller.RequestDelete(cancellationToken);

        // Assert
        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().Be(true);
    }

    [Fact]
    public async Task RequestDelete_ShouldReturnBadRequest_WhenFail()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.RequestDeleteUser(cancellationToken))
                   .ReturnsAsync(false);

        // Act
        var response = await _controller.RequestDelete(cancellationToken);

        // Assert
        var badRequest = response.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.Value.Should().Be("Unable to process delete request. User may not exist or already has a pending deletion request.");
    }

    [Fact]
    public async Task RequestDelete_ShouldCallService_WithCorrectParameters()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.RequestDeleteUser(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        // Act
        await _controller.RequestDelete(cancellationToken);

        // Assert
        _serviceMock.Verify(s => s.RequestDeleteUser(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetList_ShouldHandleEmptyResults()
    {
        // Arrange
        var input = new PagedFilteredAndSortedInput();
        var emptyPagedOutput = new PagedOutput<UserDeleteRequestDto>
        {
            Items = new List<UserDeleteRequestDto>(),
            TotalCount = 0
        };

        _serviceMock.Setup(s => s.GetList(input, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(emptyPagedOutput);

        // Act
        var result = await _controller.GetList(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task AbortDelete_ShouldHandleDifferentUserIds()
    {
        // Arrange
        var userId1 = TestUtils.Guids[0];
        var userId2 = TestUtils.Guids[1];
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(s => s.AbortDeleteUser(userId1, cancellationToken))
                   .ReturnsAsync(true);
        _serviceMock.Setup(s => s.AbortDeleteUser(userId2, cancellationToken))
                   .ReturnsAsync(false);

        // Act
        var response1 = await _controller.AbortDelete(userId1, cancellationToken);
        var response2 = await _controller.AbortDelete(userId2, cancellationToken);

        // Assert
        response1.Result.Should().BeOfType<OkObjectResult>();
        response2.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}