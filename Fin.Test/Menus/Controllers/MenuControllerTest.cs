using Fin.Api.Menus;
using Fin.Application.Menus;
using Fin.Domain.Global.Classes;
using Fin.Domain.Menus.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Fin.Test.Menus.Controllers;

public class MenuControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IMenuService> _serviceMock;
    private readonly MenuController _controller;

    public MenuControllerTest()
    {
        _serviceMock = new Mock<IMenuService>();
        _controller = new MenuController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetList_ShouldReturnPagedOutput()
    {
        // Arrange
        var input = new PagedFilteredAndSortedInput();
        var expectedOutput = new PagedOutput<MenuOutput>(1,
        [
            new MenuOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] }
        ]);

        _serviceMock.Setup(s => s.GetList(input)).ReturnsAsync(expectedOutput);

        // Act
        var result = await _controller.GetList(input);

        // Assert
        result.Should().BeEquivalentTo(expectedOutput);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenMenuExists()
    {
        // Arrange
        var menuId = TestUtils.Guids[0];
        var expectedMenu = new MenuOutput { Id = menuId, Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Get(menuId)).ReturnsAsync(expectedMenu);

        // Act
        var result = await _controller.Get(menuId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedMenu);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenMenuDoesNotExist()
    {
        // Arrange
        var menuId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Get(menuId)).ReturnsAsync((MenuOutput)null);

        // Act
        var result = await _controller.Get(menuId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new MenuInput { Name = TestUtils.Strings[1] };
        var createdMenu = new MenuOutput { Id = TestUtils.Guids[0], Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync(createdMenu);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdMenu);

        (result.Result as CreatedResult)?.Location.Should().Be($"menus/{createdMenu.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenCreationFails()
    {
        // Arrange
        var input = new MenuInput();
        _serviceMock.Setup(s => s.Create(input, true)).ReturnsAsync((MenuOutput)null);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<UnprocessableEntityResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var menuId = TestUtils.Guids[0];
        var input = new MenuInput { Name = TestUtils.Strings[1] };
        _serviceMock.Setup(s => s.Update(menuId, input, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(menuId, input);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_ShouldReturnUnprocessableEntity_WhenUpdateFails()
    {
        // Arrange
        var menuId = TestUtils.Guids[0];
        var input = new MenuInput();
        _serviceMock.Setup(s => s.Update(menuId, input, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Update(menuId, input);

        // Assert
        result.Should().BeOfType<UnprocessableEntityResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var menuId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Delete(menuId, true)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(menuId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnUnprocessableEntity_WhenDeleteFails()
    {
        // Arrange
        var menuId = TestUtils.Guids[0];
        _serviceMock.Setup(s => s.Delete(menuId, true)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(menuId);

        // Assert
        result.Should().BeOfType<UnprocessableEntityResult>();
    }
}