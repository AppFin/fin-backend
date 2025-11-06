using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using FluentAssertions;

namespace Fin.Test.Wallets;

public class WalletEntityTest : TestUtils.BaseTest
{
    #region Constructor

    [Fact]
    public void Constructor_ShouldInitializeWithInput()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = TestUtils.Guids[0],
            InitialBalance = TestUtils.Decimals[0]
        };

        // Act
        var wallet = new Wallet(input);

        // Assert
        wallet.Should().NotBeNull();
        wallet.Name.Should().Be(input.Name);
        wallet.Color.Should().Be(input.Color);
        wallet.Icon.Should().Be(input.Icon);
        wallet.FinancialInstitutionId.Should().Be(input.FinancialInstitutionId);
        wallet.InitialBalance.Should().Be(input.InitialBalance);
        wallet.Inactivated.Should().BeFalse();
        wallet.Titles.Should().BeEmpty();
        wallet.CreditCards.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNullFinancialInstitution()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = TestUtils.Decimals[0]
        };

        // Act
        var wallet = new Wallet(input);

        // Assert
        wallet.Should().NotBeNull();
        wallet.FinancialInstitutionId.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithZeroBalance()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 0m
        };

        // Act
        var wallet = new Wallet(input);

        // Assert
        wallet.Should().NotBeNull();
        wallet.InitialBalance.Should().Be(0m);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var originalInput = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = TestUtils.Guids[0],
            InitialBalance = TestUtils.Decimals[0]
        };
        var wallet = new Wallet(originalInput);

        var updateInput = new WalletInput
        {
            Name = TestUtils.Strings[3],
            Color = TestUtils.Strings[4],
            Icon = TestUtils.Strings[5],
            FinancialInstitutionId = TestUtils.Guids[1],
            InitialBalance = TestUtils.Decimals[1]
        };

        // Act
        wallet.Update(updateInput);

        // Assert
        wallet.Name.Should().Be(updateInput.Name);
        wallet.Color.Should().Be(updateInput.Color);
        wallet.Icon.Should().Be(updateInput.Icon);
        wallet.FinancialInstitutionId.Should().Be(updateInput.FinancialInstitutionId);
        wallet.InitialBalance.Should().Be(updateInput.InitialBalance);
    }

    [Fact]
    public void Update_ShouldAllowChangingToNullFinancialInstitution()
    {
        // Arrange
        var originalInput = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = TestUtils.Guids[0],
            InitialBalance = TestUtils.Decimals[0]
        };
        var wallet = new Wallet(originalInput);

        var updateInput = new WalletInput
        {
            Name = TestUtils.Strings[3],
            Color = TestUtils.Strings[4],
            Icon = TestUtils.Strings[5],
            FinancialInstitutionId = null,
            InitialBalance = TestUtils.Decimals[1]
        };

        // Act
        wallet.Update(updateInput);

        // Assert
        wallet.FinancialInstitutionId.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldAllowChangingInitialBalance()
    {
        // Arrange
        var originalInput = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(originalInput);

        var updateInput = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 2000m
        };

        // Act
        wallet.Update(updateInput);

        // Assert
        wallet.InitialBalance.Should().Be(2000m);
    }

    #endregion

    #region ToggleInactivated

    [Fact]
    public void ToggleInactivated_ShouldChangeFromFalseToTrue()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = TestUtils.Decimals[0]
        };
        var wallet = new Wallet(input);
        wallet.Inactivated.Should().BeFalse();

        // Act
        wallet.ToggleInactivated();

        // Assert
        wallet.Inactivated.Should().BeTrue();
    }

    [Fact]
    public void ToggleInactivated_ShouldChangeFromTrueToFalse()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = TestUtils.Decimals[0]
        };
        var wallet = new Wallet(input);
        wallet.ToggleInactivated(); // Set to true
        wallet.Inactivated.Should().BeTrue();

        // Act
        wallet.ToggleInactivated();

        // Assert
        wallet.Inactivated.Should().BeFalse();
    }

    [Fact]
    public void ToggleInactivated_ShouldToggleMultipleTimes()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = TestUtils.Decimals[0]
        };
        var wallet = new Wallet(input);

        // Act & Assert
        wallet.Inactivated.Should().BeFalse();
        
        wallet.ToggleInactivated();
        wallet.Inactivated.Should().BeTrue();
        
        wallet.ToggleInactivated();
        wallet.Inactivated.Should().BeFalse();
        
        wallet.ToggleInactivated();
        wallet.Inactivated.Should().BeTrue();
    }

    #endregion

    #region CalculateBalanceAt

    [Fact]
    public void CalculateBalanceAt_ShouldReturnZero_WhenDateIsBeforeCreation()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[5];

        var dateBeforeCreation = TestUtils.UtcDateTimes[0]; // Earlier date

        // Act
        var balance = wallet.CalculateBalanceAt(dateBeforeCreation);

        // Assert
        balance.Should().Be(0m);
    }

    [Fact]
    public void CalculateBalanceAt_ShouldReturnInitialBalance_WhenNoTitlesExist()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[0];

        // Act
        var balance = wallet.CalculateBalanceAt(TestUtils.UtcDateTimes[5]);

        // Assert
        balance.Should().Be(1000m);
    }

    [Fact]
    public void CalculateBalanceAt_ShouldReturnInitialBalance_WhenNoTitlesBeforeDate()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[0];

        // Add title after the query date
        var futureTitle = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[5],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        wallet.Titles.Add(futureTitle);

        // Act - Query before the title date
        var balance = wallet.CalculateBalanceAt(TestUtils.UtcDateTimes[2]);

        // Assert
        balance.Should().Be(1000m); // Should return initial balance
    }

    [Fact]
    public void CalculateBalanceAt_ShouldReturnCorrectBalance_WithSingleIncomeTitle()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[0];

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[2],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        wallet.Titles.Add(title);

        // Act
        var balance = wallet.CalculateBalanceAt(TestUtils.UtcDateTimes[5]);

        // Assert
        balance.Should().Be(1500m); // 1000 + 500
    }

    [Fact]
    public void CalculateBalanceAt_ShouldReturnCorrectBalance_WithSingleExpenseTitle()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[0];

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 300m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[2],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        wallet.Titles.Add(title);

        // Act
        var balance = wallet.CalculateBalanceAt(TestUtils.UtcDateTimes[5]);

        // Assert
        balance.Should().Be(700m); // 1000 - 300
    }

    [Fact]
    public void CalculateBalanceAt_ShouldReturnCorrectBalance_WithMultipleTitles()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[0];

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[2],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1500m);

        var title3 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 300m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[3],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1300m);

        wallet.Titles.Add(title1);
        wallet.Titles.Add(title2);
        wallet.Titles.Add(title3);

        // Act
        var balance = wallet.CalculateBalanceAt(TestUtils.UtcDateTimes[5]);

        // Assert
        balance.Should().Be(1600m); // 1000 + 500 - 200 + 300
    }

    [Fact]
    public void CalculateBalanceAt_ShouldReturnLastTitleBalance_WhenMultipleTitlesOnSameDate()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[0];

        var sameDate = TestUtils.UtcDateTimes[2];

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = sameDate,
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
        }, 1000m);
        title1.Id = TestUtils.Guids[0];

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = sameDate,
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1500m);
        title2.Id = TestUtils.Guids[1];

        wallet.Titles.Add(title1);
        wallet.Titles.Add(title2);

        // Act
        var balance = wallet.CalculateBalanceAt(TestUtils.UtcDateTimes[5]);

        // Assert
        // Should return the last title's balance (ordered by Date desc)
        balance.Should().Be(1300m); // Last title2: 1500 - 200
    }

    [Fact]
    public void CalculateBalanceAt_ShouldOnlyConsiderTitlesUpToDate()
    {
        // Arrange
        var input = new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            FinancialInstitutionId = null,
            InitialBalance = 1000m
        };
        var wallet = new Wallet(input);
        wallet.CreatedAt = TestUtils.UtcDateTimes[0];

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[2],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1500m);

        var title3 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 300m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[5], // Future title
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1300m);

        wallet.Titles.Add(title1);
        wallet.Titles.Add(title2);
        wallet.Titles.Add(title3);

        // Act - Query at date between title2 and title3
        var balance = wallet.CalculateBalanceAt(TestUtils.UtcDateTimes[3]);

        // Assert
        balance.Should().Be(1300m); // Should only consider title1 and title2
    }

    #endregion
}