using System.Security;
using Fin.Infrastructure.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fin.Test.Errors;

public class ExceptionHandlingMiddlewareTest
{
    [Fact]
    public async Task Invoke_Should_CallNext_When_NoException()
    {
        // Arrange
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.Invoke(context);

        // Assert
        wasCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Invoke_Should_Return401_When_UnauthorizedAccessException()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw new UnauthorizedAccessException();

        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var body = await GetResponseBody(context.Response);
        body.Should().Contain("You are not authorized");
    }

    [Fact]
    public async Task Invoke_Should_Return403_When_SecurityException()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw new SecurityException();

        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var body = await GetResponseBody(context.Response);
        body.Should().Contain("forbidden");
    }

    [Fact]
    public async Task Invoke_Should_Return404_When_KeyNotFoundException()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw new KeyNotFoundException();

        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var body = await GetResponseBody(context.Response);
        body.Should().Contain("not found");
    }

    [Fact]
    public async Task Invoke_Should_Return500_And_Log_When_UnhandledException()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var exception = new InvalidOperationException("Something broke!");
        RequestDelegate next = _ => throw exception;

        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = await GetResponseBody(context.Response);
        body.Should().Contain("unexpected error");

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Unhandled exception")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> GetResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
}