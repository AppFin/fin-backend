using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Fin.Test.Authentications;

public class ActivatedMiddlewareTest: TestUtils.BaseTestWithContext
{
    private readonly Mock<IAmbientData> _mockAmbientData;
    private readonly ActivatedMiddleware _middleware;

    public ActivatedMiddlewareTest()
    {
        _mockAmbientData = new Mock<IAmbientData>();
        _middleware = new ActivatedMiddleware(_mockAmbientData.Object, GetRepository<User>());
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_UserNotLogged()
    {
        // Arrange
        _mockAmbientData.Setup(x => x.IsLogged).Returns(false);

        var context = new DefaultHttpContext();
        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        _mockAmbientData.Verify(x => x.IsLogged, Times.Once);
        _mockAmbientData.Verify(x => x.UserId, Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_UserIsLoggedAndActive()
    {
        // Arrange
        var userId = TestUtils.Guids[0];

        _mockAmbientData.Setup(x => x.IsLogged).Returns(true);
        _mockAmbientData.Setup(x => x.UserId).Returns(userId);

        // Create and save an active user
        var activeUser = new User
        {
            Id = userId,
            Tenants = [new() { Id = TestUtils.Guids[1] }],
            Credential = new UserCredential()
        };
        if (!activeUser.IsActivity) activeUser.ToggleActivity();

        await Context.Users.AddAsync(activeUser);
        await Context.SaveChangesAsync();

        var context = new DefaultHttpContext();
        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        _mockAmbientData.Verify(x => x.IsLogged, Times.Once);
        _mockAmbientData.Verify(x => x.UserId, Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return401_When_UserIsLoggedButInactive()
    {
        // Arrange
        var userId = TestUtils.Guids[0];

        _mockAmbientData.Setup(x => x.IsLogged).Returns(true);
        _mockAmbientData.Setup(x => x.UserId).Returns(userId);

        // Create and save an inactive user
        var inactiveUser = new User
        {
            Id = userId,
            Tenants = [new() { Id = TestUtils.Guids[1] }],
            Credential = new UserCredential()
        };
        if (inactiveUser.IsActivity) inactiveUser.ToggleActivity();

        await Context.Users.AddAsync(inactiveUser);
        await Context.SaveChangesAsync();

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream(); // necessário para capturar o WriteAsync

        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _mockAmbientData.Verify(x => x.IsLogged, Times.Once);
        _mockAmbientData.Verify(x => x.UserId, Times.Once);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("Invalid user, deleted or inactive user");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return401_When_UserIsLoggedButDoesNotExist()
    {
        // Arrange
        var userId = TestUtils.Guids[0]; // User that doesn't exist in database

        _mockAmbientData.Setup(x => x.IsLogged).Returns(true);
        _mockAmbientData.Setup(x => x.UserId).Returns(userId);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream(); // necessário para capturar o WriteAsync

        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _mockAmbientData.Verify(x => x.IsLogged, Times.Once);
        _mockAmbientData.Verify(x => x.UserId, Times.Once);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("Invalid user, deleted or inactive user");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return401_When_UserIsLoggedAndActiveButDifferentUserId()
    {
        // Arrange
        var existingUserId = TestUtils.Guids[0];
        var ambientUserId = TestUtils.Guids[1]; // Different user ID in ambient data

        _mockAmbientData.Setup(x => x.IsLogged).Returns(true);
        _mockAmbientData.Setup(x => x.UserId).Returns(ambientUserId);

        // Create and save an active user with different ID
        var activeUser = new User
        {
            Id = existingUserId, // Different from ambient user ID
            Tenants = [new() { Id = TestUtils.Guids[2] }],
            Credential = new UserCredential()
        };
        if (!activeUser.IsActivity) activeUser.ToggleActivity();

        await Context.Users.AddAsync(activeUser);
        await Context.SaveChangesAsync();

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream(); // necessário para capturar o WriteAsync

        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _mockAmbientData.Verify(x => x.IsLogged, Times.Once);
        _mockAmbientData.Verify(x => x.UserId, Times.Once);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("Invalid user, deleted or inactive user");
    }
}