using Fin.Api.TitleCategories;
using Fin.Application.TitleCategories;
using Fin.Application.TitleCategories.Dtos;
using Fin.Application.TitleCategories.Enums;
using Fin.Domain.Global.Classes;
using Fin.Domain.TitleCategories.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Fin.Application.Globals.Dtos;

namespace Fin.Test.TitleCategories.Controllers;

public class TitleCategoryControllerTest : TestUtils.BaseTest
{
    private readonly Mock<ITitleCategoryService> _serviceMock;
    private readonly TitleCategoryController _controller;

    public TitleCategoryControllerTest()
    {
        _serviceMock = new Mock<ITitleCategoryService>();
        _controller = new TitleCategoryController(_serviceMock.Object);
    }

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedOutput()
    {
        // Arrange
        var input = new TitleCategoryGetListInput();
        var expectedOutput = new PagedOutput<TitleCategoryOutput>(1,
        [
            new TitleCategoryOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] }
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
    public async Task Get_ShouldReturnOk_WhenCategoryExists()
    {
        // Arrange
        var categoryId = TestUtils.Guids[0];
        var expectedCategory = new TitleCategoryOutput { Id = categoryId, Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Get(categoryId)).ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.Get(categoryId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedCategory);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Get(categoryId)).ReturnsAsync((TitleCategoryOutput)null);

        // Act
        var result = await _controller.Get(categoryId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new TitleCategoryInput { Name = TestUtils.Strings[1], Color = TestUtils.Strings[2], Icon = TestUtils.Strings[3] };
        var createdCategory = new TitleCategoryOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] };
        var successResult = new ValidationResultDto<TitleCategoryOutput, TitleCategoryCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = createdCategory
        };

        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdCategory);

        (result.Result as CreatedResult)?.Location.Should().Be($"categories/{createdCategory.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenCreationFails()
    {
        // Arrange
        var input = new TitleCategoryInput();
        var failureResult = new ValidationResultDto<TitleCategoryOutput, TitleCategoryCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = TitleCategoryCreateOrUpdateErrorCode.NameIsRequired,
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
        var categoryId = TestUtils.Guids[0];
        var input = new TitleCategoryInput { Name = TestUtils.Strings[1], Color = TestUtils.Strings[2], Icon = TestUtils.Strings[3] };
        var successResult = new ValidationResultDto<bool, TitleCategoryCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = true
        };

        _serviceMock.Setup(s => s.Update(categoryId, input, true)).ReturnsAsync(successResult);

        // Act
        var result = await _controller.Update(categoryId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = TestUtils.Guids[0];
        var input = new TitleCategoryInput { Name = TestUtils.Strings[1], Color = TestUtils.Strings[2], Icon = TestUtils.Strings[3] };
        var notFoundResult = new ValidationResultDto<bool, TitleCategoryCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = TitleCategoryCreateOrUpdateErrorCode.TitleCategoryNotFound,
            Message = "Title category not found to edit."
        };

        _serviceMock.Setup(s => s.Update(categoryId, input, true)).ReturnsAsync(notFoundResult);

        // Act
        var result = await _controller.Update(categoryId, input);

        // Assert
        var notFoundObjectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundObjectResult.Value.Should().BeEquivalentTo(notFoundResult);
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenUpdateFailsForOtherReasons()
    {
        // Arrange
        var categoryId = TestUtils.Guids[0];
        var input = new TitleCategoryInput();
        var failureResult = new ValidationResultDto<bool, TitleCategoryCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = TitleCategoryCreateOrUpdateErrorCode.NameAlreadyInUse,
            Message = "Name is already in use."
        };

        _serviceMock.Setup(s => s.Update(categoryId, input, true)).ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(categoryId, input);

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
        var categoryId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.ToggleInactive(categoryId, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.ToggleInactivated(categoryId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ToggleInactivated_ShouldReturnNotFound_WhenToggleFails()
    {
        // Arrange
        var categoryId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.ToggleInactive(categoryId, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.ToggleInactivated(categoryId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var categoryId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Delete(categoryId, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(categoryId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
    {
        // Arrange
        var categoryId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Delete(categoryId, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(categoryId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}