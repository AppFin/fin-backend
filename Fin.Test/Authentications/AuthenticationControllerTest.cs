using Fin.Api.Authentication;
using Fin.Application.Authentications.Dtos;
using Fin.Application.Authentications.Enums;
using Fin.Application.Authentications.Services;
using Fin.Application.Authentications.Utils;
using Fin.Application.Globals.Dtos;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fin.Test.Authentications;

public class AuthenticationControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<IAuthenticationHelper> _authHelperMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IConfigurationSection> _demoModeSectionMock;
    private readonly AuthenticationController _controller;

    public AuthenticationControllerTest()
    {
        _authServiceMock = new Mock<IAuthenticationService>();
        _authHelperMock = new Mock<IAuthenticationHelper>();
        _configurationMock = new Mock<IConfiguration>();
        _demoModeSectionMock = new Mock<IConfigurationSection>();
        SetDemoMode(false);
        _controller = new AuthenticationController(_authServiceMock.Object, _authHelperMock.Object, _configurationMock.Object);
    }

    private void SetDemoMode(bool enabled)
    {
        _demoModeSectionMock.Setup(s => s.Value).Returns(enabled.ToString());
        _configurationMock
            .Setup(c => c.GetSection(AppConstants.DemoModeConfigKey))
            .Returns(_demoModeSectionMock.Object);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var input = new LoginInput { Email = "user@email.com", Password = "123456" };
        var expected = new LoginOutput { Success = true };

        _authServiceMock.Setup(s => s.Login(input)).ReturnsAsync(expected);

        // Act
        var result = await _controller.Login(input);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Login_ShouldReturnUnprocessableEntity_WhenFail()
    {
        // Arrange
        var input = new LoginInput { Email = "fail@email.com", Password = "wrong" };
        var expected = new LoginOutput { Success = false };

        _authServiceMock.Setup(s => s.Login(input)).ReturnsAsync(expected);

        // Act
        var result = await _controller.Login(input);

        // Assert
        result.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var input = new RefreshTokenInput { RefreshToken = "valid" };
        var expected = new LoginOutput { Success = true };

        _authServiceMock.Setup(s => s.RefreshToken("valid")).ReturnsAsync(expected);

        // Act
        var result = await _controller.RefreshToken(input);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnprocessableEntity_WhenFail()
    {
        // Arrange
        var input = new RefreshTokenInput { RefreshToken = "invalid" };
        var expected = new LoginOutput { Success = false };

        _authServiceMock.Setup(s => s.RefreshToken("invalid")).ReturnsAsync(expected);

        // Act
        var result = await _controller.RefreshToken(input);

        // Assert
        result.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task LoggedOut_ShouldReturnOk()
    {
        // Arrange
        var token = "Bearer abc.def.ghi";
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = token;

        _controller.ControllerContext = new ControllerContext { HttpContext = context };

        _authServiceMock.Setup(s => s.Logout("abc.def.ghi")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.LoggedOut();

        // Assert
        _authServiceMock.Verify(s => s.Logout("abc.def.ghi"), Times.Once);
    }

    [Fact]
    public void LoginGoogle_ShouldReturnChallenge()
    {
        // Arrange + Act
        var result = _controller.LoginGoogle();

        // Assert
        result.Should().BeOfType<ChallengeResult>();
        var challenge = result as ChallengeResult;
        challenge!.AuthenticationSchemes.Should().Contain(GoogleDefaults.AuthenticationScheme);
    }

    [Fact]
    public async Task StartResetPassword_ShouldReturnOk()
    {
        // Arrange
        var input = new SendResetPasswordEmailInput { Email = "user@email.com" };
        _authServiceMock.Setup(s => s.SendResetPasswordEmail(input)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StartResetPassword(input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var input = new ResetPasswordInput { ResetToken = TestUtils.Strings[0], PasswordConfirmation = TestUtils.Strings[1], Password = TestUtils.Strings[2] };
        var output = new ValidationResultDto<bool, ResetPasswordErrorCode>
        {
            Success = true,
            Message = "ok",
            Data = true
        };

        _authServiceMock.Setup(s => s.ResetPassword(input)).ReturnsAsync(output);

        // Act
        var result = await _controller.ResetPassword(input);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().Be(true);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnUnprocessableEntity_WhenFail()
    {
        // Arrange
        var input = new ResetPasswordInput { ResetToken = TestUtils.Strings[0], PasswordConfirmation = TestUtils.Strings[1], Password = TestUtils.Strings[2] };
        var output = new ValidationResultDto<bool, ResetPasswordErrorCode>
        {
            Success = false,
            Message = "fail",
            Data = false
        };

        _authServiceMock.Setup(s => s.ResetPassword(input)).ReturnsAsync(output);

        // Act
        var result = await _controller.ResetPassword(input);

        // Assert
        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public void LoginGoogle_ShouldReturnNotFound_WhenDemoModeEnabled()
    {
        // Arrange
        SetDemoMode(true);

        // Act
        var result = _controller.LoginGoogle();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GoogleCallback_ShouldReturnNotFound_WhenDemoModeEnabled()
    {
        // Arrange
        SetDemoMode(true);

        // Act
        var result = await _controller.GoogleCallback();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _authHelperMock.Verify(h => h.GenerateCallbackResponse(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<LoginOutput>()), Times.Never);
    }

    [Fact]
    public async Task StartResetPassword_ShouldReturnNotFound_WhenDemoModeEnabled()
    {
        // Arrange
        SetDemoMode(true);
        var input = new SendResetPasswordEmailInput { Email = "user@email.com" };

        // Act
        var result = await _controller.StartResetPassword(input);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _authServiceMock.Verify(s => s.SendResetPasswordEmail(It.IsAny<SendResetPasswordEmailInput>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnNotFound_WhenDemoModeEnabled()
    {
        // Arrange
        SetDemoMode(true);
        var input = new ResetPasswordInput { ResetToken = TestUtils.Strings[0], PasswordConfirmation = TestUtils.Strings[1], Password = TestUtils.Strings[2] };

        // Act
        var result = await _controller.ResetPassword(input);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _authServiceMock.Verify(s => s.ResetPassword(It.IsAny<ResetPasswordInput>()), Times.Never);
    }
}
