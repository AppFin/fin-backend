using Fin.Api.Users;
using Fin.Application.Globals.Dtos;
using Fin.Application.Users.Dtos;
using Fin.Application.Users.Enums;
using Fin.Application.Users.Services;
using Fin.Domain.Users.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Users;

public class UserCreateControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IUserCreateService> _serviceMock;
    private readonly UserCreateController _controller;

    public UserCreateControllerTest()
    {
        _serviceMock = new Mock<IUserCreateService>();
        _controller = new UserCreateController(_serviceMock.Object);
    }

    [Fact]
    public async Task StartCreate_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var input = new UserStartCreateInput { Email = "test@email.com" };
        var output = new UserStartCreateOutput { CreationToken = "abc123" };
        var result = new ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>
        {
            Success = true,
            Data = output,
            Message = "OK"
        };

        _serviceMock.Setup(s => s.StartCreate(input)).ReturnsAsync(result);

        // Act
        var response = await _controller.StartCreate(input);

        // Assert
        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeEquivalentTo(output);
    }

    [Fact]
    public async Task StartCreate_ShouldReturnUnprocessable_WhenFail()
    {
        // Arrange
        var input = new UserStartCreateInput { Email = "fail@email.com" };
        var result = new ValidationResultDto<UserStartCreateOutput, UserStartCreateErrorCode>
        {
            Success = false,
            Message = "Erro de validação"
        };

        _serviceMock.Setup(s => s.StartCreate(input)).ReturnsAsync(result);

        // Act
        var response = await _controller.StartCreate(input);

        // Assert
        response.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task ResendEmail_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var token = "token123";
        var now = DateTime.UtcNow;

        var result = new ValidationResultDto<DateTime>
        {
            Success = true,
            Data = now,
            Message = "Enviado"
        };

        _serviceMock.Setup(s => s.ResendConfirmationEmail(token)).ReturnsAsync(result);

        // Act
        var response = await _controller.ResendEmail(token);

        // Assert
        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ((DateTime)ok!.Value!).Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ResendEmail_ShouldReturnUnprocessable_WhenFail()
    {
        // Arrange
        var token = "invalid-token";
        var result = new ValidationResultDto<DateTime>
        {
            Success = false,
            Message = "Token inválido"
        };

        _serviceMock.Setup(s => s.ResendConfirmationEmail(token)).ReturnsAsync(result);

        // Act
        var response = await _controller.ResendEmail(token);

        // Assert
        response.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task ValidEmail_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var token = "valid-token";
        var code = "123456";

        var result = new ValidationResultDto<bool>
        {
            Success = true,
            Data = true,
            Message = "Código válido"
        };

        _serviceMock.Setup(s => s.ValidateEmailCode(token, code)).ReturnsAsync(result);

        // Act
        var response = await _controller.ValidEmail(token, code);

        // Assert
        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().Be(true);
    }

    [Fact]
    public async Task ValidEmail_ShouldReturnUnprocessable_WhenFail()
    {
        // Arrange
        var token = "invalid-token";
        var code = "wrong-code";

        var result = new ValidationResultDto<bool>
        {
            Success = false,
            Message = "Código inválido"
        };

        _serviceMock.Setup(s => s.ValidateEmailCode(token, code)).ReturnsAsync(result);

        // Act
        var response = await _controller.ValidEmail(token, code);

        // Assert
        response.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated_WhenSuccess()
    {
        // Arrange
        var token = "token";
        var input = new UserUpdateOrCreateInput { DisplayName = "Test User" };
        var user = new UserDto { Id = TestUtils.Guids[1], DisplayName = "Test User" };

        var result = new ValidationResultDto<UserDto>
        {
            Success = true,
            Data = user,
            Message = "Usuário criado"
        };

        _serviceMock.Setup(s => s.CreateUser(token, input)).ReturnsAsync(result);

        // Act
        var response = await _controller.CreateUser(token, input);

        // Assert
        var created = response.Result as CreatedResult;
        created.Should().NotBeNull();
        created!.Value.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnUnprocessable_WhenFail()
    {
        // Arrange
        var token = "invalid";
        var input = new UserUpdateOrCreateInput { DisplayName = "Fail" };

        var result = new ValidationResultDto<UserDto>
        {
            Success = false,
            Message = "Erro ao criar"
        };

        _serviceMock.Setup(s => s.CreateUser(token, input)).ReturnsAsync(result);

        // Act
        var response = await _controller.CreateUser(token, input);

        // Assert
        response.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }
}
