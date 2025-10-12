using Fin.Api.FinancialInstitutions;
using Fin.Application.FinancialInstitutions;
using Fin.Application.FinancialInstitutions.Dtos;
using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.Global.Classes;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.FinancialInstitutions.Controllers;

public class FinancialInstitutionControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IFinancialInstitutionService> _serviceMock;
    private readonly FinancialInstitutionController _controller;

    public FinancialInstitutionControllerTest()
    {
        _serviceMock = new Mock<IFinancialInstitutionService>();
        _controller = new FinancialInstitutionController(_serviceMock.Object);
    }

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedOutput()
    {
        // Arrange
        var input = new FinancialInstitutionGetListInput();
        var expectedOutput = new PagedOutput<FinancialInstitutionOutput>(1,
        [
            new FinancialInstitutionOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] }
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
    public async Task Get_ShouldReturnOk_WhenInstitutionExists()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        var expectedInstitution = new FinancialInstitutionOutput { Id = institutionId, Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Get(institutionId)).ReturnsAsync(expectedInstitution);

        // Act
        var result = await _controller.Get(institutionId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedInstitution);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenInstitutionDoesNotExist()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Get(institutionId)).ReturnsAsync((FinancialInstitutionOutput)null);

        // Act
        var result = await _controller.Get(institutionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenCreationSucceeds()
    {
        // Arrange
        var input = new FinancialInstitutionInput { Name = TestUtils.Strings[1] };
        var createdInstitution = new FinancialInstitutionOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync(createdInstitution);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdInstitution);

        (result.Result as CreatedResult)?.Location.Should().Be($"financial-institutions/{createdInstitution.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenCreationFails()
    {
        // Arrange
        var input = new FinancialInstitutionInput();
        // Null return from service can indicate internal validation failure (BadHttpRequestException)
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync((FinancialInstitutionOutput)null);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<UnprocessableEntityResult>();
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        var input = new FinancialInstitutionInput { Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Update(institutionId, input, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(institutionId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenInstitutionDoesNotExist()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        var input = new FinancialInstitutionInput();
        _serviceMock.Setup(s => s.Update(institutionId, input, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Update(institutionId, input);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Delete(institutionId, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(institutionId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        // Service returns false if not found or if it has relations
        _serviceMock.Setup(s => s.Delete(institutionId, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(institutionId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region ToggleInactive

    [Fact]
    public async Task ToggleInactive_ShouldReturnOk_WhenToggleSucceeds()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.ToggleInactive(institutionId, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.ToggleInactive(institutionId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnNotFound_WhenToggleFails()
    {
        // Arrange
        var institutionId = TestUtils.Guids[0];
        // Service returns false if not found or if it has active wallets
        _serviceMock.Setup(s => s.ToggleInactive(institutionId, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.ToggleInactive(institutionId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}