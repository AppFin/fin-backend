using Fin.Application.Menus;
using Fin.Domain.Global.Classes;
using Fin.Domain.Menus.Dtos;
using Fin.Domain.Menus.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.Menus.Services;

public class MenuServiceTest : TestUtils.BaseTestWithContext
{
    #region Get

    [Fact]
    public async Task Get_ShouldReturnMenu_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var menu = new Menu(new MenuInput { Name = TestUtils.Strings[0], FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.MenuRepository.AddAsync(menu, true);

        // Act
        var result = await service.Get(menu.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(menu.Id);
        result.Name.Should().Be(menu.Name);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Get(TestUtils.Guids[9]);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await resources.MenuRepository.AddAsync(new Menu(new MenuInput { Name = "A", FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);
        await resources.MenuRepository.AddAsync(new Menu(new MenuInput { Name = "B", FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);

        var input = new PagedFilteredAndSortedInput { MaxResultCount = 1, SkipCount = 1 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("B");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new MenuInput
        {
            Name = TestUtils.Strings[0],
            FrontRoute = TestUtils.Strings[0],
            Icon = TestUtils.Strings[0]
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        var dbMenu = await resources.MenuRepository.Query(false).FirstOrDefaultAsync(a => a.Id == result.Id);
        dbMenu.Should().NotBeNull();
        dbMenu.Name.Should().Be(input.Name);
        dbMenu.FrontRoute.Should().Be(input.FrontRoute);
        dbMenu.Icon.Should().Be(input.Icon);
    }

    [Fact]
    public async Task Create_NameRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new MenuInput
        {
            FrontRoute = TestUtils.Strings[0],
            Icon = TestUtils.Strings[0]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Create(input, true));
        exception.Message.Should().Be("Name is required");
    }

    [Fact]
    public async Task Create_FrontRouteRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new MenuInput
        {
            Name = TestUtils.Strings[0],
            Icon = TestUtils.Strings[0]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Create(input, true));
        exception.Message.Should().Be("FrontRoute is required");
    }

    [Fact]
    public async Task Create_IconRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new MenuInput
        {
            Name = TestUtils.Strings[0],
            FrontRoute = TestUtils.Strings[0]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Create(input, true));
        exception.Message.Should().Be("Icon is required");
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnFalse_WhenMenuNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Update(TestUtils.Guids[9], new MenuInput{ FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3], Name = TestUtils.Strings[0]}, true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var menu = new Menu(new MenuInput { Name = TestUtils.Strings[0], FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.MenuRepository.AddAsync(menu, true);

        var input = new MenuInput { Name = TestUtils.Strings[4], FrontRoute = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act
        var result = await service.Update(menu.Id, input, true);

        // Assert
        result.Should().BeTrue();

        var dbMenu = await resources.MenuRepository.Query(false).FirstOrDefaultAsync(a => a.Id == menu.Id);
        dbMenu.Should().NotBeNull();
        dbMenu.Name.Should().Be(input.Name);
        dbMenu.FrontRoute.Should().Be(input.FrontRoute);
        dbMenu.Icon.Should().Be(input.Icon);
    }

    [Fact]
    public async Task Update_ShouldReturnThrow_NameRequired()
    {

        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var menu = new Menu(new MenuInput { Name = TestUtils.Strings[0], FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.MenuRepository.AddAsync(menu, true);

        var input = new MenuInput { FrontRoute = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act & asser
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Update(menu.Id, input, true));
        exception.Message.Should().Be("Name is required");
    }

    [Fact]
    public async Task Update_ShouldReturnThrow_FrontRouteRequired()
    {

        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var menu = new Menu(new MenuInput { Name = TestUtils.Strings[0], FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.MenuRepository.AddAsync(menu, true);

        var input = new MenuInput { Name = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act & asser
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Update(menu.Id, input, true));
        exception.Message.Should().Be("FrontRoute is required");
    }

    [Fact]
    public async Task Update_ShouldReturnThrow_InconRequired()
    {

        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var menu = new Menu(new MenuInput { Name = TestUtils.Strings[0], FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.MenuRepository.AddAsync(menu, true);

        var input = new MenuInput { Name = TestUtils.Strings[5], FrontRoute = TestUtils.Strings[6] };

        // Act & asser
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Update(menu.Id, input, true));
        exception.Message.Should().Be("Icon is required");
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnFalse_WhenMenuNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Delete(TestUtils.Guids[9], true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShoulReturnTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var menu = new Menu(new MenuInput { Name = TestUtils.Strings[0], FrontRoute = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.MenuRepository.AddAsync(menu, true);

        // Act
        var result = await service.Delete(menu.Id, true);

        // Assert
        result.Should().BeTrue();
        (await resources.MenuRepository.Query(false).FirstOrDefaultAsync(a => a.Id == menu.Id)).Should().BeNull();
    }

    #endregion

    private MenuService GetService(Resources resources)
    {
        return new MenuService(resources.MenuRepository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            MenuRepository = GetRepository<Menu>()
        };
    }

    private class Resources
    {
        public IRepository<Menu> MenuRepository { get; set; }
    }
}