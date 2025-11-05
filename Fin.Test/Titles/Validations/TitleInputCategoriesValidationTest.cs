using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Validations.UpdateOrCrestes;
using Fin.Domain.TitleCategories.Dtos;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.TitleCategories.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.Titles.Validations;

public class TitleInputCategoriesValidationTest : TestUtils.BaseTestWithContext
{
    private TitleInput GetValidInput() => new()
    {
        Description = TestUtils.Strings[0],
        Value = TestUtils.Decimals[0],
        Date = TestUtils.UtcDateTimes[0],
        WalletId = TestUtils.Guids[0],
        Type = TitleType.Income,
        TitleCategoriesIds = new List<Guid>()
    };

    private async Task<TitleCategory> CreateCategoryInDatabase(
        Resources resources,
        Guid id,
        TitleCategoryType type,
        string name,
        bool inactivated = false)
    {
        var category = new TitleCategory(new TitleCategoryInput
        {
            Type = type,
            Name = name,
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2]
        });
        
        
        category.Id = id;
        
        if (inactivated != category.Inactivated)
            category.ToggleInactivated();

        await resources.CategoryRepository.AddAsync(category, autoSave: true);
        return category;
    }
    
    private async Task<Title> CreateTitleInDatabase(
        Resources resources, 
        Guid id, 
        List<TitleCategory> categories)
    {
        var input = new TitleInput
        {
            Value = TestUtils.Decimals[0],
            Date = TestUtils.UtcDateTimes[0],
            Type = TitleType.Income,
            Description = TestUtils.Strings[0],
            TitleCategoriesIds = categories.Select(c => c.Id).ToList()
        };
        
        var title = new Title(input, 0m);
        title.Wallet = TestUtils.Wallets[0];
        title.Id = id;
        await resources.TitleRepository.AddAsync(title, autoSave: true);
        
        // Eager load TitleCategories for verification
        return await resources.TitleRepository
            .Include(t => t.TitleCategories)
            .FirstAsync(t => t.Id == id);
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenCategoriesAreValid()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var cat1 = await CreateCategoryInDatabase(resources, TestUtils.Guids[1], TitleCategoryType.Income, TestUtils.Strings[1]);
        var cat2 = await CreateCategoryInDatabase(resources, TestUtils.Guids[2], TitleCategoryType.Both, TestUtils.Strings[0]);

        var input = GetValidInput();
        input.TitleCategoriesIds.Add(cat1.Id);
        input.TitleCategoriesIds.Add(cat2.Id);

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Code.Should().BeNull();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSomeCategoriesNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var cat1 = await CreateCategoryInDatabase(resources, TestUtils.Guids[1], TitleCategoryType.Income, TestUtils.Strings[1]);
        var notFoundId = TestUtils.Guids[9];

        var input = GetValidInput();
        input.TitleCategoriesIds.Add(cat1.Id);
        input.TitleCategoriesIds.Add(notFoundId);

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomeCategoriesNotFound);
        result.Data.Should().HaveCount(1);
        result.Data.Should().BeEquivalentTo(new List<Guid> { notFoundId });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSomeCategoriesInactiveOnCreate()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var cat1 = await CreateCategoryInDatabase(resources, TestUtils.Guids[1], TitleCategoryType.Income, TestUtils.Strings[1]);
        var cat2_Inactive = await CreateCategoryInDatabase(resources, TestUtils.Guids[2], TitleCategoryType.Income, TestUtils.Strings[0], inactivated: true);

        var input = GetValidInput();
        input.TitleCategoriesIds.Add(cat1.Id);
        input.TitleCategoriesIds.Add(cat2_Inactive.Id);

        // Act
        var result = await service.ValidateAsync(input, editingId: null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomeCategoriesInactive);
        result.Data.Should().HaveCount(1);
        result.Data.Should().BeEquivalentTo(new List<Guid> { cat2_Inactive.Id });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSomeCategoriesHasIncompatibleTypes()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var cat1_Income = await CreateCategoryInDatabase(resources, TestUtils.Guids[1], TitleCategoryType.Income, TestUtils.Strings[0]);
        var cat2_Expense = await CreateCategoryInDatabase(resources, TestUtils.Guids[2], TitleCategoryType.Expense, TestUtils.Strings[1]);
        var cat3_All = await CreateCategoryInDatabase(resources, TestUtils.Guids[3], TitleCategoryType.Both, TestUtils.Strings[2]);

        var input = GetValidInput();
        input.Type = TitleType.Income; // Input is Income
        input.TitleCategoriesIds.Add(cat1_Income.Id);
        input.TitleCategoriesIds.Add(cat2_Expense.Id); // Incompatible
        input.TitleCategoriesIds.Add(cat3_All.Id);

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomeCategoriesHasIncompatibleTypes);
        result.Data.Should().HaveCount(1);
        result.Data.Should().BeEquivalentTo(new List<Guid> { cat2_Expense.Id });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenCategoryIsInactiveButAlreadyOnTitle()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var cat1_Inactive = await CreateCategoryInDatabase(resources, TestUtils.Guids[1], TitleCategoryType.Income, TestUtils.Strings[1], inactivated: true);
        var title = await CreateTitleInDatabase(resources, TestUtils.Guids[5], new List<TitleCategory> { cat1_Inactive });
        
        var input = GetValidInput();
        input.TitleCategoriesIds.Add(cat1_Inactive.Id);

        // Act
        var result = await service.ValidateAsync(input, editingId: title.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenAddingNewInactiveCategoryOnUpdate()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var cat1_Active = await CreateCategoryInDatabase(resources, TestUtils.Guids[1], TitleCategoryType.Income, TestUtils.Strings[0]);
        var cat2_Inactive = await CreateCategoryInDatabase(resources, TestUtils.Guids[2], TitleCategoryType.Income, TestUtils.Strings[1], inactivated: true);
        var title = await CreateTitleInDatabase(resources, TestUtils.Guids[5], new List<TitleCategory> { cat1_Active });
        
        var input = GetValidInput();
        input.TitleCategoriesIds.Add(cat1_Active.Id);
        input.TitleCategoriesIds.Add(cat2_Inactive.Id); // Adding a new inactive category

        // Act
        var result = await service.ValidateAsync(input, editingId: title.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomeCategoriesInactive);
        result.Data.Should().HaveCount(1);
        result.Data.Should().BeEquivalentTo(new List<Guid> { cat2_Inactive.Id });
    }

    #endregion

    private TitleInputCategoriesValidation GetService(Resources resources)
    {
        return new TitleInputCategoriesValidation(
            resources.TitleRepository,
            resources.CategoryRepository
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleRepository = GetRepository<Title>(),
            CategoryRepository = GetRepository<TitleCategory>()
        };
    }

    private class Resources
    {
        public IRepository<Title> TitleRepository { get; set; }
        public IRepository<TitleCategory> CategoryRepository { get; set; }
    }
}