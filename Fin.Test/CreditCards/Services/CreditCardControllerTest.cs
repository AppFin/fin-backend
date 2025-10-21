using Fin.Api.CreditCards;
using Fin.Application.CreditCards.Dtos;
using Fin.Application.CreditCards.Enums;
using Fin.Application.CreditCards.Services;
using Fin.Application.Globals.Dtos;
using Fin.Domain.CreditCards.Dtos;
using Fin.Domain.Global.Classes;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.CreditCards.Services;

public class CreditCardControllerTest : TestUtils.BaseTest
{
    private readonly Mock<ICreditCardService> _serviceMock;
    private readonly CreditCardController _controller;

    public CreditCardControllerTest()
    {
        _serviceMock = new Mock<ICreditCardService>();
        _controller = new CreditCardController(_serviceMock.Object);
    }

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedOutput()
    {
        // Arrange
        var input = new CreditCardGetListInput();
        var expectedOutput = new PagedOutput<CreditCardOutput>(1,
        [
            new CreditCardOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] }
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
    public async Task Get_ShouldReturnOk_WhenCreditCardExists()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var expectedCard = new CreditCardOutput { Id = cardId, Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Get(cardId)).ReturnsAsync(expectedCard);

        // Act
        var result = await _controller.Get(cardId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedCard);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenCreditCardDoesNotExist()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Get(cardId)).ReturnsAsync((CreditCardOutput)null);

        // Act
        var result = await _controller.Get(cardId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new CreditCardInput { Name = TestUtils.Strings[1], Limit = 5000m };
        var createdCard = new CreditCardOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] };
        var successResult = new ValidationResultDto<CreditCardOutput, CreditCardCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = createdCard
        };
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdCard);
            
        // NOTE: The controller uses 'categories/{validationResult.Data?.Id}' as location, replicating the provided controller code.
        (result.Result as CreatedResult)?.Location.Should().Be($"categories/{createdCard.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var input = new CreditCardInput();
        var failureResult = new ValidationResultDto<CreditCardOutput, CreditCardCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardCreateOrUpdateErrorCode.NameIsRequired,
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
        var cardId = TestUtils.Guids[0];
        var input = new CreditCardInput { Name = TestUtils.Strings[1], Limit = 5000m };
        var successResult = new ValidationResultDto<bool, CreditCardCreateOrUpdateErrorCode> { Success = true, Data = true };
        _serviceMock.Setup(s => s.Update(cardId, input, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Update(cardId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenCreditCardDoesNotExist()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var input = new CreditCardInput();
        var notFoundResult = new ValidationResultDto<bool, CreditCardCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardCreateOrUpdateErrorCode.CreditCardNotFound,
            Message = "CreditCard not found to edit."
        };
        _serviceMock.Setup(s => s.Update(cardId, input, true)).ReturnsAsync(notFoundResult);

        // Act
        var result = await _controller.Update(cardId, input);

        // Assert
        var notFoundObjectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundObjectResult.Value.Should().BeEquivalentTo(notFoundResult);
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var input = new CreditCardInput();
        var failureResult = new ValidationResultDto<bool, CreditCardCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardCreateOrUpdateErrorCode.NameAlreadyInUse,
            Message = "Name is already in use."
        };
        _serviceMock.Setup(s => s.Update(cardId, input, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(cardId, input);

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
        var cardId = TestUtils.Guids[0];
        var successResult = new ValidationResultDto<bool, CreditCardToggleInactiveErrorCode> { Success = true, Data = true };
        _serviceMock.Setup(s => s.ToggleInactive(cardId, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.ToggleInactivated(cardId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ToggleInactivated_ShouldReturnNotFound_WhenCreditCardDoesNotExist()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var notFoundResult = new ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardToggleInactiveErrorCode.CreditCardNotFound,
            Message = "CreditCard not found."
        };
        _serviceMock.Setup(s => s.ToggleInactive(cardId, true)).ReturnsAsync(notFoundResult);

        // Act
        var result = await _controller.ToggleInactivated(cardId);

        // Assert
        var notFoundObjectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundObjectResult.Value.Should().BeEquivalentTo(notFoundResult);
    }

    [Fact]
    public async Task ToggleInactivated_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var failureResult = new ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardToggleInactiveErrorCode.CreditCardNotFound, // Reusing error code for a general failure scenario
            Message = "General validation failed."
        };
        _serviceMock.Setup(s => s.ToggleInactive(cardId, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.ToggleInactivated(cardId);

        // Assert
        var unprocessableEntityResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        unprocessableEntityResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var successResult = new ValidationResultDto<bool, CreditCardDeleteErrorCode> { Success = true, Data = true };
        _serviceMock.Setup(s => s.Delete(cardId, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Delete(cardId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenCreditCardDoesNotExist()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var notFoundResult = new ValidationResultDto<bool, CreditCardDeleteErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardDeleteErrorCode.CreditCardNotFound,
            Message = "CreditCard not found."
        };
        _serviceMock.Setup(s => s.Delete(cardId, true)).ReturnsAsync(notFoundResult);

        // Act
        var result = await _controller.Delete(cardId);

        // Assert
        var notFoundObjectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundObjectResult.Value.Should().BeEquivalentTo(notFoundResult);
    }

    [Fact]
    public async Task Delete_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var cardId = TestUtils.Guids[0];
        var failureResult = new ValidationResultDto<bool, CreditCardDeleteErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardDeleteErrorCode.CreditCardInUse, // Reusing error code for a general failure scenario
            Message = "Cannot delete due to related transactions."
        };
        _serviceMock.Setup(s => s.Delete(cardId, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Delete(cardId);

        // Assert
        var unprocessableEntityResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableEntityResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion
}