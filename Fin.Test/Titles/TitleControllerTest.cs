using Fin.Api.Titles;
using Fin.Application.Globals.Dtos;
using Fin.Application.Titles.Dtos;
using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Titles;

public class TitleControllerTest : TestUtils.BaseTest
{
    private readonly Mock<ITitleService> _serviceMock;
    private readonly TitleController _controller;

    public TitleControllerTest()
    {
        _serviceMock = new Mock<ITitleService>();
        _controller = new TitleController(_serviceMock.Object);
    }

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult()
    {
        // Arrange
        var input = new TitleGetListInput
        {
            SkipCount = 0,
            MaxResultCount = 10
        };

        var expectedResult = new PagedOutput<TitleOutput>
        {
            Items = new List<TitleOutput>
            {
                new()
                {
                    Id = TestUtils.Guids[0],
                    Description = TestUtils.Strings[0],
                    Value = TestUtils.Decimals[0]
                }
            },
            TotalCount = 1
        };

        _serviceMock
            .Setup(s => s.GetList(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items.First().Id.Should().Be(TestUtils.Guids[0]);
    }

    [Fact]
    public async Task GetList_ShouldReturnEmptyResult_WhenNoTitlesExist()
    {
        // Arrange
        var input = new TitleGetListInput
        {
            SkipCount = 0,
            MaxResultCount = 10
        };

        var expectedResult = new PagedOutput<TitleOutput>
        {
            Items = new List<TitleOutput>(),
            TotalCount = 0
        };

        _serviceMock
            .Setup(s => s.GetList(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region Get

    [Fact]
    public async Task Get_ShouldReturnOk_WhenTitleExists()
    {
        // Arrange
        var titleId = TestUtils.Guids[0];
        var expectedTitle = new TitleOutput
        {
            Id = titleId,
            Description = TestUtils.Strings[0],
            Value = TestUtils.Decimals[0],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0]
        };

        _serviceMock
            .Setup(s => s.Get(titleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTitle);

        // Act
        var result = await _controller.Get(titleId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenTitleDoesNotExist()
    {
        // Arrange
        var titleId = TestUtils.Guids[0];

        _serviceMock
            .Setup(s => s.Get(titleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TitleOutput?)null);

        // Act
        var result = await _controller.Get(titleId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new TitleInput
        {
            Description = TestUtils.Strings[0],
            Value = TestUtils.Decimals[0],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };

        var createdTitle = new TitleOutput
        {
            Id = TestUtils.Guids[2],
            Description = input.Description,
            Value = input.Value,
            Type = input.Type,
            Date = input.Date
        };

        var successResult = new ValidationResultDto<TitleOutput, TitleCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = createdTitle
        };

        _serviceMock
            .Setup(s => s.Create(input, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdTitle);

        var createdResult = result.Result as CreatedResult;
        createdResult!.Location.Should().Be($"categories/{createdTitle.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var input = new TitleInput
        {
            Description = TestUtils.Strings[0],
            Value = TestUtils.Decimals[0],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        var failureResult = new ValidationResultDto<TitleOutput, TitleCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = TitleCreateOrUpdateErrorCode.DescriptionIsRequired,
            Message = "Description is required."
        };

        _serviceMock
            .Setup(s => s.Create(input, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        var unprocessableResult = result.Result
            .Should().BeOfType<UnprocessableEntityObjectResult>()
            .Subject;
        unprocessableResult.Value.Should().BeEquivalentTo(failureResult);
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenWalletNotFound()
    {
        // Arrange
        var input = new TitleInput
        {
            Description = TestUtils.Strings[0],
            Value = TestUtils.Decimals[0],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        var failureResult = new ValidationResultDto<TitleOutput, TitleCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = TitleCreateOrUpdateErrorCode.WalletNotFound,
            Message = "Wallet not found."
        };

        _serviceMock
            .Setup(s => s.Create(input, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        var unprocessableResult = result.Result
            .Should().BeOfType<UnprocessableEntityObjectResult>()
            .Subject;
        unprocessableResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateIsSuccessful()
    {
        // Arrange
        var titleId = TestUtils.Guids[0];
        var input = new TitleInput
        {
            Description = TestUtils.Strings[1],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = TestUtils.Guids[1],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[2] }
        };

        var successResult = new ValidationResultDto<bool, TitleCreateOrUpdateErrorCode>
        {
            Success = true
        };

        _serviceMock
            .Setup(s => s.Update(titleId, input, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var result = await _controller.Update(titleId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenTitleDoesNotExist()
    {
        // Arrange
        var titleId = TestUtils.Guids[0];
        var input = new TitleInput
        {
            Description = TestUtils.Strings[1],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = TestUtils.Guids[1],
            TitleCategoriesIds = new List<Guid>()
        };

        var failureResult = new ValidationResultDto<bool, TitleCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = TitleCreateOrUpdateErrorCode.TitleNotFound,
            Message = "Title not found."
        };

        _serviceMock
            .Setup(s => s.Update(titleId, input, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(titleId, input);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(failureResult);
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var titleId = TestUtils.Guids[0];
        var input = new TitleInput
        {
            Description = TestUtils.Strings[1],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = TestUtils.Guids[1],
            TitleCategoriesIds = new List<Guid>()
        };

        var failureResult = new ValidationResultDto<bool, TitleCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = TitleCreateOrUpdateErrorCode.DescriptionIsRequired,
            Message = "Description is required."
        };

        _serviceMock
            .Setup(s => s.Update(titleId, input, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(titleId, input);

        // Assert
        var unprocessableResult = result
            .Should().BeOfType<UnprocessableEntityObjectResult>()
            .Subject;
        unprocessableResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleteIsSuccessful()
    {
        // Arrange
        var titleId = TestUtils.Guids[0];

        var successResult = new ValidationResultDto<bool, TitleDeleteErrorCode>
        {
            Success = true
        };

        _serviceMock
            .Setup(s => s.Delete(titleId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var result = await _controller.Delete(titleId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenTitleDoesNotExist()
    {
        // Arrange
        var titleId = TestUtils.Guids[0];

        var failureResult = new ValidationResultDto<bool, TitleDeleteErrorCode>
        {
            Success = false,
            ErrorCode = TitleDeleteErrorCode.TitleNotFound,
            Message = "Title not found to delete."
        };

        _serviceMock
            .Setup(s => s.Delete(titleId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Delete(titleId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion
}