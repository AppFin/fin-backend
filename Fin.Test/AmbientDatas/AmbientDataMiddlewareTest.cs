using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fin.Infrastructure.AmbientDatas;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Fin.Test.AmbientDatas;

public class AmbientDataMiddlewareTest
{
    [Fact(DisplayName = "Invoke should SetNotLogged when no authorization header")]
    public async Task InvokeAsync_Should_SetNotLogged_When_NoAuthorizationHeader()
    {
        // Arrange
        var mockAmbientData = new Mock<IAmbientData>();
        var middleware = new AmbientDataMiddleware(mockAmbientData.Object);

        var context = new DefaultHttpContext();
        var nextCalled = false;

        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }
        
        // Act
        await middleware.InvokeAsync(context, Next);

        // Assert
        mockAmbientData.Verify(a => a.SetNotLogged(), Times.Once);
        mockAmbientData.Verify(a => a.SetData(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        nextCalled.Should().BeTrue();
    }

    
    [Fact(DisplayName = "InvokeAsync should SetNotLogged when header does not start with bearer")]
    public async Task InvokeAsync_Should_SetNotLogged_When_HeaderDoesNotStartWithBearer()
    {
        // Arrange
        var mockAmbientData = new Mock<IAmbientData>();
        var middleware = new AmbientDataMiddleware(mockAmbientData.Object);

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic abcdefghijk";

        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        mockAmbientData.Verify(a => a.SetNotLogged(), Times.Once);
        mockAmbientData.Verify(a => a.SetData(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        nextCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "InvokeAsync should SetData when token is invalid")]
    public async Task InvokeAsync_Should_SetData_When_TokenIsValid()
    {
        // Arrange
        var mockAmbientData = new Mock<IAmbientData>();
        var middleware = new AmbientDataMiddleware(mockAmbientData.Object);

        var claims = new Dictionary<string, string>
        {
            { "userId", TestUtils.Guids[0].ToString() },
            { "unique_name", "Test User" },
            { "role", "Admin" },
            { "tenantId", TestUtils.Guids[1].ToString() }
        };

        var token = GenerateJwtToken(claims);

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        // Act
        await middleware.InvokeAsync(context, next);

        // Assert
        mockAmbientData.Verify(a => a.SetData(
            Guid.Parse(claims["tenantId"]),
            Guid.Parse(claims["userId"]),
            "Test User",
            true), Times.Once);

        mockAmbientData.Verify(a => a.SetNotLogged(), Times.Never);
        nextCalled.Should().BeTrue();
    }

    
    private static string GenerateJwtToken(Dictionary<string, string> claims)
    {
        var token = new JwtSecurityToken(
            claims: claims.Select(c => new Claim(c.Key, c.Value)),
            signingCredentials: null
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}