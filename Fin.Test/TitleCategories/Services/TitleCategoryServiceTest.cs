using Fin.Application.TitleCategories;
using Fin.Application.TitleCategories.Dtos;
using Fin.Domain.TitleCategories.Dtos;
using Fin.Domain.TitleCategories.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.TitleCategories.Services;

public class TitleCategoryServiceTest : TestUtils.BaseTestWithContext
{
    #region Get

    [Fact]
    public async Task Get_ShouldReturnTitleCategory_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        // Act
        var result = await service.Get(titleCategory.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(titleCategory.Id);
        result.Name.Should().Be(titleCategory.Name);
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
    public async Task GetList_ShouldReturnPagedResult_WithoutFilter()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await resources.TitleCategoryRepository.AddAsync(new TitleCategory(new TitleCategoryInput { Name = "C", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);
        await resources.TitleCategoryRepository.AddAsync(new TitleCategory(new TitleCategoryInput { Name = "A", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);
        await resources.TitleCategoryRepository.AddAsync(new TitleCategory(new TitleCategoryInput { Name = "B", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);

        var input = new TitleCategoryGetListInput { MaxResultCount = 2, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        // Default sort is Inactivated (false first) then Name (asc). All are not inactivated, so sort by Name.
        result.Items.First().Name.Should().Be("A");
        result.Items.Last().Name.Should().Be("B");
    }

    [Fact]
    public async Task GetList_ShouldFilterByInactivatedTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await resources.TitleCategoryRepository.AddAsync(new TitleCategory(new TitleCategoryInput { Name = "Active1", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);
        var inactive = new TitleCategory(new TitleCategoryInput { Name = "Inactive1", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        inactive.ToggleInactivated();
        await resources.TitleCategoryRepository.AddAsync(inactive, true);
        var inactive2 = new TitleCategory(new TitleCategoryInput { Name = "Inactive2", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        inactive2.ToggleInactivated();
        await resources.TitleCategoryRepository.AddAsync(inactive2, true);

        var input = new TitleCategoryGetListInput { Inactivated = true, MaxResultCount = 10, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("Inactive1"); // Sorted by Name asc
        result.Items.Last().Name.Should().Be("Inactive2");
    }

    [Fact]
    public async Task GetList_ShouldFilterByInactivatedFalse()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await resources.TitleCategoryRepository.AddAsync(new TitleCategory(new TitleCategoryInput { Name = "Active1", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);
        var inactive = new TitleCategory(new TitleCategoryInput { Name = "Inactive1", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        inactive.ToggleInactivated();
        await resources.TitleCategoryRepository.AddAsync(inactive, true);
        await resources.TitleCategoryRepository.AddAsync(new TitleCategory(new TitleCategoryInput { Name = "Active2", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] }), true);

        var input = new TitleCategoryGetListInput { Inactivated = false, MaxResultCount = 10, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("Active1"); // Sorted by Name asc
        result.Items.Last().Name.Should().Be("Active2");
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

        var input = new TitleCategoryInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2]
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        var dbTitleCategory = await resources.TitleCategoryRepository.Query(false).FirstOrDefaultAsync(a => a.Id == result.Id);
        dbTitleCategory.Should().NotBeNull();
        dbTitleCategory.Name.Should().Be(input.Name);
        dbTitleCategory.Color.Should().Be(input.Color);
        dbTitleCategory.Icon.Should().Be(input.Icon);
        dbTitleCategory.Inactivated.Should().BeFalse();
    }

    [Fact]
    public async Task Create_NameRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new TitleCategoryInput
        {
            Color = TestUtils.Strings[0],
            Icon = TestUtils.Strings[0]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Create(input, true));
        exception.Message.Should().Be("Name is required");
    }

    [Fact]
    public async Task Create_ColorRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new TitleCategoryInput
        {
            Name = TestUtils.Strings[0],
            Icon = TestUtils.Strings[0]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Create(input, true));
        // Note: The service validates 'Color' but throws an error message 'FrontRoute is required'
        exception.Message.Should().Be("FrontRoute is required"); 
    }

    [Fact]
    public async Task Create_IconRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);

        var input = new TitleCategoryInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[0]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Create(input, true));
        exception.Message.Should().Be("Icon is required");
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnFalse_WhenTitleCategoryNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Update(TestUtils.Guids[9], new TitleCategoryInput{ Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], Name = TestUtils.Strings[0]}, true);

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
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Name = TestUtils.Strings[4], Color = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act
        var result = await service.Update(titleCategory.Id, input, true);

        // Assert
        result.Should().BeTrue();

        var dbTitleCategory = await resources.TitleCategoryRepository.Query(false).FirstOrDefaultAsync(a => a.Id == titleCategory.Id);
        dbTitleCategory.Should().NotBeNull();
        dbTitleCategory.Name.Should().Be(input.Name);
        dbTitleCategory.Color.Should().Be(input.Color);
        dbTitleCategory.Icon.Should().Be(input.Icon);
    }

    [Fact]
    public async Task Update_ShouldThrow_NameRequired()
    {

        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Color = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act & asser
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Update(titleCategory.Id, input, true));
        exception.Message.Should().Be("Name is required");
    }

    [Fact]
    public async Task Update_ShouldThrow_ColorRequired()
    {

        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Name = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act & asser
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Update(titleCategory.Id, input, true));
        exception.Message.Should().Be("FrontRoute is required");
    }

    [Fact]
    public async Task Update_ShouldThrow_IconRequired()
    {

        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Name = TestUtils.Strings[5], Color = TestUtils.Strings[6] };

        // Act & asser
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await service.Update(titleCategory.Id, input, true));
        exception.Message.Should().Be("Icon is required");
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnFalse_WhenTitleCategoryNotFound()
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
    public async Task Delete_ShouldReturnTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        // Act
        var result = await service.Delete(titleCategory.Id, true);

        // Assert
        result.Should().BeTrue();
        (await resources.TitleCategoryRepository.Query(false).FirstOrDefaultAsync(a => a.Id == titleCategory.Id)).Should().BeNull();
    }

    #endregion

    #region ToggleInactive

    [Fact]
    public async Task ToggleInactive_ShouldReturnFalse_WhenTitleCategoryNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.ToggleInactive(TestUtils.Guids[9], true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleInactive_ShouldDeactivate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);
        titleCategory.Inactivated.Should().BeFalse();

        // Act
        var result = await service.ToggleInactive(titleCategory.Id, true);

        // Assert
        result.Should().BeTrue();
        var dbTitleCategory = await resources.TitleCategoryRepository.Query(false).FirstOrDefaultAsync(a => a.Id == titleCategory.Id);
        dbTitleCategory.Should().NotBeNull();
        dbTitleCategory.Inactivated.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReactivate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        DateTimeProvider.Setup(d => d.UtcNow()).Returns(TestUtils.UtcDateTimes[0]);
        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        titleCategory.ToggleInactivated();
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);
        titleCategory.Inactivated.Should().BeTrue();

        // Act
        var result = await service.ToggleInactive(titleCategory.Id, true);

        // Assert
        result.Should().BeTrue();
        var dbTitleCategory = await resources.TitleCategoryRepository.Query(false).FirstOrDefaultAsync(a => a.Id == titleCategory.Id);
        dbTitleCategory.Should().NotBeNull();
        dbTitleCategory.Inactivated.Should().BeFalse();
    }

    #endregion

    private TitleCategoryService GetService(Resources resources)
    {
        return new TitleCategoryService(resources.TitleCategoryRepository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleCategoryRepository = GetRepository<TitleCategory>()
        };
    }

    private class Resources
    {
        public IRepository<TitleCategory> TitleCategoryRepository { get; set; }
    }
}