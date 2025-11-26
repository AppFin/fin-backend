using Fin.Application.Globals.Dtos;
using Fin.Application.TitleCategories;
using Fin.Application.TitleCategories.Dtos;
using Fin.Application.TitleCategories.Enums;
using Fin.Domain.TitleCategories.Dtos;
using Fin.Domain.TitleCategories.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
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
        result.Items.First().Name.Should().Be("Inactive1");
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
        result.Items.First().Name.Should().Be("Active1");
        result.Items.Last().Name.Should().Be("Active2");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnSuccessAndTitleCategory_WhenInputIsValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

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
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var dbTitleCategory = await resources.TitleCategoryRepository.AsNoTracking().FirstOrDefaultAsync(a => a.Id == result.Data.Id);
        dbTitleCategory.Should().NotBeNull();
        dbTitleCategory.Name.Should().Be(input.Name);
        dbTitleCategory.Color.Should().Be(input.Color);
        dbTitleCategory.Icon.Should().Be(input.Icon);
        dbTitleCategory.Inactivated.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_NameRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new TitleCategoryInput
        {
            Color = TestUtils.Strings[0],
            Icon = TestUtils.Strings[0]
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.NameIsRequired);
        result.Message.Should().Be("Name is required.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_ColorRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new TitleCategoryInput
        {
            Name = TestUtils.Strings[0],
            Icon = TestUtils.Strings[0]
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.ColorIsRequired);
        result.Message.Should().Be("Color is required.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_IconRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new TitleCategoryInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[0]
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.IconIsRequired);
        result.Message.Should().Be("Icon is required.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_NameAlreadyInUse()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var existingName = TestUtils.Strings[0];
        await resources.TitleCategoryRepository.AddAsync(new TitleCategory(new TitleCategoryInput { Name = existingName, Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2] }), true);

        var input = new TitleCategoryInput
        {
            Name = existingName,
            Color = TestUtils.Strings[3],
            Icon = TestUtils.Strings[4]
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.NameAlreadyInUse);
        result.Message.Should().Be("Name is already in use.");
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenTitleCategoryNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var input = new TitleCategoryInput { Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], Name = TestUtils.Strings[0] };

        // Act
        var result = await service.Update(TestUtils.Guids[9], input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.TitleCategoryNotFound);
        result.Message.Should().Be("Title category not found to edit.");
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnSuccessAndTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Name = TestUtils.Strings[4], Color = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act
        var result = await service.Update(titleCategory.Id, input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var dbTitleCategory = await resources.TitleCategoryRepository.AsNoTracking().FirstAsync(a => a.Id == titleCategory.Id);
        dbTitleCategory.Name.Should().Be(input.Name);
        dbTitleCategory.Color.Should().Be(input.Color);
        dbTitleCategory.Icon.Should().Be(input.Icon);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_NameRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Color = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act
        var result = await service.Update(titleCategory.Id, input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.NameIsRequired);
        result.Message.Should().Be("Name is required.");
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_ColorRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Name = TestUtils.Strings[5], Icon = TestUtils.Strings[6] };

        // Act
        var result = await service.Update(titleCategory.Id, input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.ColorIsRequired);
        result.Message.Should().Be("Color is required.");
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_IconRequired()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        var input = new TitleCategoryInput { Name = TestUtils.Strings[5], Color = TestUtils.Strings[6] };

        // Act
        var result = await service.Update(titleCategory.Id, input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.IconIsRequired);
        result.Message.Should().Be("Icon is required.");
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_NameAlreadyInUse()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var existingTitleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2] });
        await resources.TitleCategoryRepository.AddAsync(existingTitleCategory, true);

        var titleCategoryToUpdate = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[3], Color = TestUtils.Strings[4], Icon = TestUtils.Strings[5] });
        await resources.TitleCategoryRepository.AddAsync(titleCategoryToUpdate, true);

        var input = new TitleCategoryInput
        {
            Name = existingTitleCategory.Name, // Name already in use by another category
            Color = TestUtils.Strings[6],
            Icon = TestUtils.Strings[7]
        };

        // Act
        var result = await service.Update(titleCategoryToUpdate.Id, input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCategoryCreateOrUpdateErrorCode.NameAlreadyInUse);
        result.Message.Should().Be("Name is already in use.");
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

        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);

        // Act
        var result = await service.Delete(titleCategory.Id, true);

        // Assert
        result.Should().BeTrue();
        (await resources.TitleCategoryRepository.AsNoTracking().FirstOrDefaultAsync(a => a.Id == titleCategory.Id)).Should().BeNull();
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

        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);
        titleCategory.Inactivated.Should().BeFalse();

        // Act
        var result = await service.ToggleInactive(titleCategory.Id, true);

        // Assert
        result.Should().BeTrue();
        var dbTitleCategory = await resources.TitleCategoryRepository.AsNoTracking().FirstOrDefaultAsync(a => a.Id == titleCategory.Id);
        dbTitleCategory.Should().NotBeNull();
        dbTitleCategory.Inactivated.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReactivate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        var titleCategory = new TitleCategory(new TitleCategoryInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3] });
        titleCategory.ToggleInactivated();
        await resources.TitleCategoryRepository.AddAsync(titleCategory, true);
        titleCategory.Inactivated.Should().BeTrue();

        // Act
        var result = await service.ToggleInactive(titleCategory.Id, true);

        // Assert
        result.Should().BeTrue();
        var dbTitleCategory = await resources.TitleCategoryRepository.AsNoTracking().FirstOrDefaultAsync(a => a.Id == titleCategory.Id);
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