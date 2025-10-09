using Fin.Api.Wallets;
using Fin.Application.Globals.Dtos;
using Fin.Application.Wallets.Dtos;
using Fin.Application.Wallets.Enums;
using Fin.Application.Wallets.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Wallets.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Wallets.Controllers;

public class WalletControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IWalletService> _serviceMock;
    private readonly WalletController _controller;

    public WalletControllerTest()
    {
        _serviceMock = new Mock<IWalletService>();
        _controller = new WalletController(_serviceMock.Object);
    }

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedOutput()
    {
        // Arrange
        var input = new WalletGetListInput();
        var expectedOutput = new PagedOutput<WalletOutput>(1,
        [
            new WalletOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] }
        ]);

        _serviceMock.Setup(s => s.GetList(input)).ReturnsAsync(expectedOutput);

        // Act
        var result = await _controller.GetList(input);

        // Assert
        result.Should().BeEquivalentTo(expectedOutput);
    }

    #endregion

    #region Get

    [Fact]
    public async Task Get_ShouldReturnOk_WhenWalletExists()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var expectedWallet = new WalletOutput { Id = walletId, Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Get(walletId)).ReturnsAsync(expectedWallet);

        // Act
        var result = await _controller.Get(walletId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedWallet);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Get(walletId)).ReturnsAsync((WalletOutput)null);

        // Act
        var result = await _controller.Get(walletId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new WalletInput { Name = TestUtils.Strings[1], Color = TestUtils.Strings[2], Icon = TestUtils.Strings[3], InitialBalance = 100m };
        var createdWallet = new WalletOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] };
        var successResult = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = createdWallet
        };
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdWallet);

        (result.Result as CreatedResult)?.Location.Should().Be($"categories/{createdWallet.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var input = new WalletInput();
        var failureResult = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = WalletCreateOrUpdateErrorCode.NameIsRequired,
            Message = "Name is required."
        };
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        var unprocessableEntityResult = result.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableEntityResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var input = new WalletInput { Name = TestUtils.Strings[1], Color = TestUtils.Strings[2], Icon = TestUtils.Strings[3], InitialBalance = 100m };
        var successResult = new ValidationResultDto<bool, WalletCreateOrUpdateErrorCode> { Success = true, Data = true };
        _serviceMock.Setup(s => s.Update(walletId, input, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Update(walletId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var input = new WalletInput();
        var notFoundResult = new ValidationResultDto<bool, WalletCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = WalletCreateOrUpdateErrorCode.WalletNotFound,
            Message = "Wallet not found to edit."
        };
        _serviceMock.Setup(s => s.Update(walletId, input, true)).ReturnsAsync(notFoundResult);

        // Act
        var result = await _controller.Update(walletId, input);

        // Assert
        var notFoundObjectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundObjectResult.Value.Should().BeEquivalentTo(notFoundResult);
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var input = new WalletInput();
        var failureResult = new ValidationResultDto<bool, WalletCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = WalletCreateOrUpdateErrorCode.NameAlreadyInUse,
            Message = "Name is already in use."
        };
        _serviceMock.Setup(s => s.Update(walletId, input, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(walletId, input);

        // Assert
        var unprocessableEntityResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableEntityResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion

    #region ToggleInactivated

    [Fact]
    public async Task ToggleInactivated_ShouldReturnOk_WhenToggleSucceeds()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var successResult = new ValidationResultDto<bool, WalletToggleInactiveErrorCode> { Success = true, Data = true };
        _serviceMock.Setup(s => s.ToggleInactive(walletId, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.ToggleInactivated(walletId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ToggleInactivated_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var notFoundResult = new ValidationResultDto<bool, WalletToggleInactiveErrorCode>
        {
            Success = false,
            ErrorCode = WalletToggleInactiveErrorCode.WalletNotFound,
            Message = "Wallet not found."
        };
        _serviceMock.Setup(s => s.ToggleInactive(walletId, true)).ReturnsAsync(notFoundResult);

        // Act
        var result = await _controller.ToggleInactivated(walletId);

        // Assert
        var notFoundObjectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundObjectResult.Value.Should().BeEquivalentTo(notFoundResult);
    }

    [Fact]
    public async Task ToggleInactivated_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var failureResult = new ValidationResultDto<bool, WalletToggleInactiveErrorCode>
        {
            Success = false,
            ErrorCode = WalletToggleInactiveErrorCode.WalletInUseByActivatedCreditCards,
            Message = "Cannot inactive due to related items."
        };
        _serviceMock.Setup(s => s.ToggleInactive(walletId, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.ToggleInactivated(walletId);

        // Assert
        var unprocessableEntityResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableEntityResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var successResult = new ValidationResultDto<bool, WalletDeleteErrorCode> { Success = true, Data = true };
        _serviceMock.Setup(s => s.Delete(walletId, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Delete(walletId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var notFoundResult = new ValidationResultDto<bool, WalletDeleteErrorCode>
        {
            Success = false,
            ErrorCode = WalletDeleteErrorCode.WalletNotFound,
            Message = "Wallet not found."
        };
        _serviceMock.Setup(s => s.Delete(walletId, true)).ReturnsAsync(notFoundResult);

        // Act
        var result = await _controller.Delete(walletId);

        // Assert
        var notFoundObjectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundObjectResult.Value.Should().BeEquivalentTo(notFoundResult);
    }

    [Fact]
    public async Task Delete_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var failureResult = new ValidationResultDto<bool, WalletDeleteErrorCode>
        {
            Success = false,
            ErrorCode = WalletDeleteErrorCode.WalletInUseByCreditCardsAndTitle,
            Message = "Cannot delete wallet with transactions."
        };
        _serviceMock.Setup(s => s.Delete(walletId, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Delete(walletId);

        // Assert
        var unprocessableEntityResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableEntityResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion
}