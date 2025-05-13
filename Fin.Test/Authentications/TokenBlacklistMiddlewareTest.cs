using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Redis;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Fin.Test.Authentications;

public class TokenBlacklistMiddlewareTest
{
    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_TokenNotLoggedOut()
    {
        // Arrange
        var mockCache = new Mock<IRedisCacheService>();
        var middleware = new TokenBlacklistMiddleware(mockCache.Object);

        var token = "valid_token";
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {token}";


        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        mockCache.Verify(x => x.GetAsync<bool>(GetTokenKey(token)), Times.Once);
    }


    [Fact]
    public async Task InvokeAsync_Should_Return401_When_TokenIsLoggedOut()
    {
        // Arrange
        var mockCache = new Mock<IRedisCacheService>();
        var middleware = new TokenBlacklistMiddleware(mockCache.Object);

        var token = "logged_out_token";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream(); // necessário para capturar o WriteAsync
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        mockCache.Setup(x => x.GetAsync<bool>(GetTokenKey(token))).ReturnsAsync(true);

        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        mockCache.Verify(x => x.GetAsync<bool>(GetTokenKey(token)), Times.Once);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("Token has been logged out.");
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_AuthorizationHeaderMissing()
    {
        // Arrange
        var mockCache = new Mock<IRedisCacheService>();
        var middleware = new TokenBlacklistMiddleware(mockCache.Object);

        var context = new DefaultHttpContext(); // Sem header

        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeTrue();
        mockCache.Verify(x => x.GetAsync<bool>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_HeaderIsNotBearer()
    {
        // Arrange
        var mockCache = new Mock<IRedisCacheService>();
        var middleware = new TokenBlacklistMiddleware(mockCache.Object);

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic xyz";

        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeTrue();
        mockCache.Verify(x => x.GetAsync<bool>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_TokenIsEmpty()
    {
        // Arrange
        var mockCache = new Mock<IRedisCacheService>();
        var middleware = new TokenBlacklistMiddleware(mockCache.Object);

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer ";

        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        wasCalled.Should().BeTrue();
        mockCache.Verify(x => x.GetAsync<bool>(It.IsAny<string>()), Times.Once);
    }
    
    private string GetTokenKey(string token)
    {
        return $"logged-out-{token}";
    }
}