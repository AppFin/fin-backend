using Fin.Api.People;
using Fin.Application.Globals.Dtos;
using Fin.Application.People;
using Fin.Application.People.Dtos;
using Fin.Application.People.Enums;
using Fin.Domain.Global.Classes;
using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.People;

public class PersonControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IPersonService> _serviceMock;
    private readonly PersonController _controller;

    public PersonControllerTest()
    {
        _serviceMock = new Mock<IPersonService>();
        _controller = new PersonController(_serviceMock.Object);
    }

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult()
    {
        // Arrange
        var input = new PersonGetListInput { MaxResultCount = 10, SkipCount = 0 };
        var expectedResult = new PagedOutput<PersonOutput>
        {
            Items = new List<PersonOutput>
            {
                new(new Person(new PersonInput { Name = TestUtils.Strings[0] })),
                new(new Person(new PersonInput { Name = TestUtils.Strings[1] }))
            },
            TotalCount = 2
        };

        _serviceMock
            .Setup(s => s.GetList(input))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetList_ShouldReturnEmpty_WhenNoData()
    {
        // Arrange
        var input = new PersonGetListInput { MaxResultCount = 10, SkipCount = 0 };
        var expectedResult = new PagedOutput<PersonOutput>
        {
            Items = new List<PersonOutput>(),
            TotalCount = 0
        };

        _serviceMock
            .Setup(s => s.GetList(input))
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
    public async Task Get_ShouldReturnOk_WhenPersonExists()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var expectedPerson = new PersonOutput(
            new Person(new PersonInput { Name = TestUtils.Strings[0] })
            { Id = personId });

        _serviceMock
            .Setup(s => s.Get(personId))
            .ReturnsAsync(expectedPerson);

        // Act
        var result = await _controller.Get(personId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedPerson);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        _serviceMock
            .Setup(s => s.Get(personId))
            .ReturnsAsync((PersonOutput)null);

        // Act
        var result = await _controller.Get(personId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new PersonInput { Name = TestUtils.Strings[0] };
        var createdPerson = new PersonOutput(
            new Person(input) { Id = TestUtils.Guids[0] });

        var successResult = new ValidationResultDto<PersonOutput, PersonCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = createdPerson
        };

        _serviceMock
            .Setup(s => s.Create(input, true))
            .ReturnsAsync(successResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdPerson);

        var createdResult = result.Result as CreatedResult;
        createdResult.Location.Should().Be($"categories/{createdPerson.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var input = new PersonInput { Name = null };
        var failureResult = new ValidationResultDto<PersonOutput, PersonCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = PersonCreateOrUpdateErrorCode.NameIsRequired,
            Message = "Name is required"
        };

        _serviceMock
            .Setup(s => s.Create(input, true))
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
    public async Task Create_ShouldReturnUnprocessableEntity_WhenNameAlreadyInUse()
    {
        // Arrange
        var input = new PersonInput { Name = TestUtils.Strings[0] };
        var failureResult = new ValidationResultDto<PersonOutput, PersonCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = PersonCreateOrUpdateErrorCode.NameAlreadyInUse,
            Message = "Name is already in use."
        };

        _serviceMock
            .Setup(s => s.Create(input, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateIsSuccessful()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var input = new PersonInput { Name = TestUtils.Strings[1] };
        var successResult = new ValidationResultDto<bool, PersonCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = true
        };

        _serviceMock
            .Setup(s => s.Update(personId, input, true))
            .ReturnsAsync(successResult);

        // Act
        var result = await _controller.Update(personId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var input = new PersonInput { Name = TestUtils.Strings[0] };
        var failureResult = new ValidationResultDto<bool, PersonCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = PersonCreateOrUpdateErrorCode.PersonNotFound
        };

        _serviceMock
            .Setup(s => s.Update(personId, input, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(personId, input);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(failureResult);
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var input = new PersonInput { Name = null };
        var failureResult = new ValidationResultDto<bool, PersonCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = PersonCreateOrUpdateErrorCode.NameIsRequired,
            Message = "Name is required"
        };

        _serviceMock
            .Setup(s => s.Update(personId, input, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(personId, input);

        // Assert
        var unprocessableResult = result
            .Should().BeOfType<UnprocessableEntityObjectResult>()
            .Subject;
        unprocessableResult.Value.Should().BeEquivalentTo(failureResult);
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenNameAlreadyInUse()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var input = new PersonInput { Name = TestUtils.Strings[0] };
        var failureResult = new ValidationResultDto<bool, PersonCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = PersonCreateOrUpdateErrorCode.NameAlreadyInUse,
            Message = "Name is already in use."
        };

        _serviceMock
            .Setup(s => s.Update(personId, input, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Update(personId, input);

        // Assert
        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    #endregion

    #region ToggleInactivated

    [Fact]
    public async Task ToggleInactivated_ShouldReturnOk_WhenPersonExists()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        _serviceMock
            .Setup(s => s.ToggleInactive(personId, true))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ToggleInactivated(personId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ToggleInactivated_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        _serviceMock
            .Setup(s => s.ToggleInactive(personId, true))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ToggleInactivated(personId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenPersonDeleted()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var successResult = new ValidationResultDto<bool, PersonDeleteErrorCode>
        {
            Success = true,
            Data = true
        };

        _serviceMock
            .Setup(s => s.Delete(personId, true))
            .ReturnsAsync(successResult);

        // Act
        var result = await _controller.Delete(personId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var failureResult = new ValidationResultDto<bool, PersonDeleteErrorCode>
        {
            Success = false,
            ErrorCode = PersonDeleteErrorCode.PersonNotFound
        };

        _serviceMock
            .Setup(s => s.Delete(personId, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Delete(personId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(failureResult);
    }

    [Fact]
    public async Task Delete_ShouldReturnUnprocessableEntity_WhenPersonInUse()
    {
        // Arrange
        var personId = TestUtils.Guids[0];
        var failureResult = new ValidationResultDto<bool, PersonDeleteErrorCode>
        {
            Success = false,
            ErrorCode = PersonDeleteErrorCode.PersonInUse,
            Message = "Person in use."
        };

        _serviceMock
            .Setup(s => s.Delete(personId, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Delete(personId);

        // Assert
        var unprocessableResult = result
            .Should().BeOfType<UnprocessableEntityObjectResult>()
            .Subject;
        unprocessableResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion
}