using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using FluentAssertions;

namespace Fin.Test.Titles;

public class TitleTest
{
    #region Constructor

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyConstructor()
    {
        // Act
        var title = new Title();

        // Assert
        title.Should().NotBeNull();
        title.Id.Should().Be(Guid.Empty);
        title.TitleCategories.Should().NotBeNull();
        title.TitleCategories.Should().BeEmpty();
        title.TitleTitleCategories.Should().NotBeNull();
        title.TitleTitleCategories.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithInputAndPreviousBalance()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test Description",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[2] }
        };
        var previousBalance = 50m;

        // Act
        var title = new Title(input, previousBalance);

        // Assert
        title.Should().NotBeNull();
        title.Id.Should().NotBe(Guid.Empty);
        title.Value.Should().Be(input.Value);
        title.Type.Should().Be(input.Type);
        title.Description.Should().Be(input.Description);
        title.Date.Should().Be(input.Date);
        title.WalletId.Should().Be(input.WalletId);
        title.PreviousBalance.Should().Be(previousBalance);
        title.TitleTitleCategories.Should().HaveCount(2);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[1]);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[2]);
    }

    [Fact]
    public void Constructor_ShouldTrimDescription()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "  Test Description  ",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var title = new Title(input, 0);

        // Assert
        title.Description.Should().Be("Test Description");
    }

    [Fact]
    public void Constructor_ShouldRemoveDuplicateCategoryIds()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[1], TestUtils.Guids[2] }
        };

        // Act
        var title = new Title(input, 0);

        // Assert
        title.TitleTitleCategories.Should().HaveCount(2);
    }

    #endregion

    #region Getters - ResultingBalance

    [Fact]
    public void ResultingBalance_ShouldCalculateCorrectly_ForIncome()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var previousBalance = 50m;

        // Act
        var title = new Title(input, previousBalance);

        // Assert
        title.ResultingBalance.Should().Be(150m); // 50 + 100
    }

    [Fact]
    public void ResultingBalance_ShouldCalculateCorrectly_ForExpense()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 30m,
            Type = TitleType.Expense,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var previousBalance = 100m;

        // Act
        var title = new Title(input, previousBalance);

        // Assert
        title.ResultingBalance.Should().Be(70m); // 100 - 30
    }

    [Fact]
    public void ResultingBalance_ShouldBeNegative_WhenExpenseExceedsBalance()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 150m,
            Type = TitleType.Expense,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var previousBalance = 100m;

        // Act
        var title = new Title(input, previousBalance);

        // Assert
        title.ResultingBalance.Should().Be(-50m); // 100 - 150
    }

    [Fact]
    public void ResultingBalance_ShouldBeZero_WhenPreviousBalanceIsZeroAndNoValue()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 0m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var title = new Title(input, 0m);

        // Assert
        title.ResultingBalance.Should().Be(0m);
    }

    #endregion

    #region Getters - EffectiveValue

    [Fact]
    public void EffectiveValue_ShouldBePositive_ForIncome()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var title = new Title(input, 0);

        // Assert
        title.EffectiveValue.Should().Be(100m);
    }

    [Fact]
    public void EffectiveValue_ShouldBeNegative_ForExpense()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 75m,
            Type = TitleType.Expense,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var title = new Title(input, 0);

        // Assert
        title.EffectiveValue.Should().Be(-75m);
    }

    #endregion

    #region UpdateAndReturnCategoriesToRemove

    [Fact]
    public void UpdateAndReturnCategoriesToRemove_ShouldUpdateBasicProperties()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Initial",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };
        var title = new Title(initialInput, 50m);

        var updateInput = new TitleInput
        {
            Value = 200m,
            Type = TitleType.Expense,
            Description = "Updated",
            Date = DateTime.Now.AddDays(1),
            WalletId = TestUtils.Guids[2],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };
        var newPreviousBalance = 150m;

        // Act
        var result = title.UpdateAndReturnCategoriesToRemove(updateInput, newPreviousBalance);

        // Assert
        title.Value.Should().Be(200m);
        title.Type.Should().Be(TitleType.Expense);
        title.Description.Should().Be("Updated");
        title.Date.Should().Be(updateInput.Date);
        title.WalletId.Should().Be(TestUtils.Guids[2]);
        title.PreviousBalance.Should().Be(150m);
        result.Should().NotBeNull();
    }

    [Fact]
    public void UpdateAndReturnCategoriesToRemove_ShouldAddNewCategories()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[2], TestUtils.Guids[3] }
        };

        // Act
        var result = title.UpdateAndReturnCategoriesToRemove(updateInput, 0);

        // Assert
        title.TitleTitleCategories.Should().HaveCount(3);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[1]);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[2]);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[3]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateAndReturnCategoriesToRemove_ShouldRemoveCategories()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[2], TestUtils.Guids[3] }
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };

        // Act
        var result = title.UpdateAndReturnCategoriesToRemove(updateInput, 0);

        // Assert
        title.TitleTitleCategories.Should().HaveCount(1);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[1]);
        result.Should().HaveCount(2);
        result.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[2]);
        result.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[3]);
    }

    [Fact]
    public void UpdateAndReturnCategoriesToRemove_ShouldKeepExistingCategories()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[2] }
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[2] }
        };

        // Act
        var result = title.UpdateAndReturnCategoriesToRemove(updateInput, 0);

        // Assert
        title.TitleTitleCategories.Should().HaveCount(2);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[1]);
        title.TitleTitleCategories.Select(x => x.TitleCategoryId).Should().Contain(TestUtils.Guids[2]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateAndReturnCategoriesToRemove_ShouldRemoveAllCategories()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1], TestUtils.Guids[2] }
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var result = title.UpdateAndReturnCategoriesToRemove(updateInput, 0);

        // Assert
        title.TitleTitleCategories.Should().BeEmpty();
        result.Should().HaveCount(2);
    }

    #endregion

    #region MustReprocess

    [Fact]
    public void MustReprocess_ShouldReturnTrue_WhenDateChanges()
    {
        // Arrange
        var initialDate = new DateTime(2025, 1, 1);
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = initialDate,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = initialDate.AddDays(1),
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MustReprocess_ShouldReturnTrue_WhenTypeChanges()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Expense,
            Description = "Test",
            Date = title.Date,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MustReprocess_ShouldReturnTrue_WhenValueChanges()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 200m,
            Type = TitleType.Income,
            Description = "Test",
            Date = title.Date,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MustReprocess_ShouldReturnTrue_WhenWalletIdChanges()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = title.Date,
            WalletId = TestUtils.Guids[1],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MustReprocess_ShouldReturnFalse_WhenOnlyDescriptionChanges()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Initial Description",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Updated Description",
            Date = title.Date,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MustReprocess_ShouldReturnFalse_WhenOnlyCategoriesChange()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = title.Date,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[2], TestUtils.Guids[3] }
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MustReprocess_ShouldReturnFalse_WhenNothingChanges()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = DateTime.Now,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = title.Date,
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MustReprocess_ShouldReturnTrue_WhenMultiplePropertiesChange()
    {
        // Arrange
        var initialInput = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = "Test",
            Date = new DateTime(2025, 1, 1),
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };
        var title = new Title(initialInput, 0);

        var updateInput = new TitleInput
        {
            Value = 200m,
            Type = TitleType.Expense,
            Description = "Updated",
            Date = new DateTime(2025, 2, 1),
            WalletId = TestUtils.Guids[1],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var result = title.MustReprocess(updateInput);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}