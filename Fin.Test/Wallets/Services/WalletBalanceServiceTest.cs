using Fin.Application.Wallets.Services;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.Wallets.Services;

public class WalletBalanceServiceTest : TestUtils.BaseTestWithContext
{
    #region GetBalanceAt

    [Fact]
    public async Task GetBalanceAt_ShouldReturnInitialBalance_WhenNoTitlesExist()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        // Act
        var result = await service.GetBalanceAt(wallet.Id, TestUtils.UtcDateTimes[0]);

        // Assert
        result.Should().Be(1000m);
    }

    [Fact]
    public async Task GetBalanceAt_ShouldReturnZero_WhenDateIsBeforeWalletCreation()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var dateBeforeCreation = wallet.CreatedAt.AddDays(-1);

        // Act
        var result = await service.GetBalanceAt(wallet.Id, dateBeforeCreation);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceAt_ShouldCalculateCorrectly_WithIncomeTitles()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 300m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1500m);

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);

        // Act
        var result = await service.GetBalanceAt(wallet.Id, TestUtils.UtcDateTimes[1]);

        // Assert
        result.Should().Be(1800m); // 1000 + 500 + 300
    }

    [Fact]
    public async Task GetBalanceAt_ShouldCalculateCorrectly_WithExpenseTitles()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 150m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 800m);

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);

        // Act
        var result = await service.GetBalanceAt(wallet.Id, TestUtils.UtcDateTimes[1]);

        // Assert
        result.Should().Be(650m); // 1000 - 200 - 150
    }

    [Fact]
    public async Task GetBalanceAt_ShouldCalculateCorrectly_WithMixedTitles()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var income = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);

        var expense = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1500m);

        await resources.TitleRepository.AddAsync(income, autoSave: true);
        await resources.TitleRepository.AddAsync(expense, autoSave: true);

        // Act
        var result = await service.GetBalanceAt(wallet.Id, TestUtils.UtcDateTimes[1]);

        // Assert
        result.Should().Be(1300m); // 1000 + 500 - 200
    }

    #endregion

    #region GetBalanceNow

    [Fact]
    public async Task GetBalanceNow_ShouldReturnCurrentBalance()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = DateTimeProvider.Object.UtcNow().AddDays(-1),
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);

        await resources.TitleRepository.AddAsync(title, autoSave: true);

        // Act
        var result = await service.GetBalanceNow(wallet.Id);

        // Assert
        result.Should().Be(1500m); // 1000 + 500
    }

    #endregion

    #region ReprocessBalance (by WalletId)

    [Fact]
    public async Task ReprocessBalance_ByWalletId_ShouldUpdateAllTitlesBalances()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m); // Wrong balance

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m); // Wrong balance

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);

        // Act
        await service.ReprocessBalance(wallet.Id, 1000m, autoSave: true);

        // Assert
        var reprocessedTitle1 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title1.Id);
        var reprocessedTitle2 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title2.Id);

        reprocessedTitle1.PreviousBalance.Should().Be(1000m);
        reprocessedTitle1.ResultingBalance.Should().Be(1500m);

        reprocessedTitle2.PreviousBalance.Should().Be(1500m);
        reprocessedTitle2.ResultingBalance.Should().Be(1300m);
    }

    #endregion

    #region ReprocessBalance (by Wallet entity)

    [Fact]
    public async Task ReprocessBalance_ByWallet_ShouldUpdateAllTitlesBalances()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        await resources.TitleRepository.AddAsync(title1, autoSave: true);

        // Load wallet with titles
        var walletWithTitles = await resources.WalletRepository
            .Include(w => w.Titles)
            .FirstAsync(w => w.Id == wallet.Id);

        // Act
        await service.ReprocessBalance(walletWithTitles, autoSave: true);

        // Assert
        var reprocessedTitle = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title1.Id);

        reprocessedTitle.PreviousBalance.Should().Be(1000m);
        reprocessedTitle.ResultingBalance.Should().Be(1500m);
    }

    #endregion

    #region ReprocessBalance (by Titles list)

    [Fact]
    public async Task ReprocessBalance_ByTitlesList_ShouldUpdateBalancesInOrder()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 300m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        var title3 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 200m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[2],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);
        await resources.TitleRepository.AddAsync(title3, autoSave: true);

        var titles = await resources.TitleRepository
            .Where(t => t.WalletId == wallet.Id)
            .ToListAsync();

        // Act
        await service.ReprocessBalance(titles, 1000m, autoSave: true);

        // Assert
        var reprocessedTitle1 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title1.Id);
        var reprocessedTitle2 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title2.Id);
        var reprocessedTitle3 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title3.Id);

        reprocessedTitle1.PreviousBalance.Should().Be(1000m);
        reprocessedTitle1.ResultingBalance.Should().Be(1500m);

        reprocessedTitle2.PreviousBalance.Should().Be(1500m);
        reprocessedTitle2.ResultingBalance.Should().Be(1200m);

        reprocessedTitle3.PreviousBalance.Should().Be(1200m);
        reprocessedTitle3.ResultingBalance.Should().Be(1400m);
    }

    [Fact]
    public async Task ReprocessBalance_ByTitlesList_ShouldHandleEmptyList()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var emptyList = new List<Title>();

        // Act
        await service.ReprocessBalance(emptyList, 1000m, autoSave: true);

        // Assert
        var count = await resources.TitleRepository.AsNoTracking().CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region ReprocessBalanceFrom (by Title entity)

    [Fact]
    public async Task ReprocessBalanceFrom_ByTitle_ShouldReprocessFollowingTitles()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);


        await resources.TitleRepository.ExecuteDeleteAsync();
        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        title1.Id = TestUtils.Guids[0];

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m); // Wrong balance
        
        title2.Id = TestUtils.Guids[1];

        var title3 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 100m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[2],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m); // Wrong balance
        title3.Id = TestUtils.Guids[2];

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);
        await resources.TitleRepository.AddAsync(title3, autoSave: true);

        // Act
        await service.ReprocessBalanceFrom(title1, autoSave: true);

        // Assert
        var reprocessedTitle2 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title2.Id);
        var reprocessedTitle3 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title3.Id);

        reprocessedTitle2.PreviousBalance.Should().Be(1500m); // title1.ResultingBalance
        reprocessedTitle2.ResultingBalance.Should().Be(1300m);

        reprocessedTitle3.PreviousBalance.Should().Be(1300m); // title2.ResultingBalance
        reprocessedTitle3.ResultingBalance.Should().Be(1400m);
    }

    #endregion

    #region ReprocessBalanceFrom (by TitleId)

    [Fact]
    public async Task ReprocessBalanceFrom_ByTitleId_ShouldReprocessFollowingTitles()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m); // Wrong balance

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);

        // Act
        await service.ReprocessBalanceFrom(title1.Id, autoSave: true);

        // Assert
        var reprocessedTitle2 = await resources.TitleRepository.AsNoTracking()
            .FirstAsync(t => t.Id == title2.Id);

        reprocessedTitle2.PreviousBalance.Should().Be(1500m);
        reprocessedTitle2.ResultingBalance.Should().Be(1300m);
    }

    #endregion

    private WalletBalanceService GetService(Resources resources)
    {
        return new WalletBalanceService(
            resources.WalletRepository,
            resources.TitleRepository,
            DateTimeProvider.Object,
            UnitOfWork
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            WalletRepository = GetRepository<Wallet>(),
            TitleRepository = GetRepository<Title>()
        };
    }

    private class Resources
    {
        public IRepository<Wallet> WalletRepository { get; set; }
        public IRepository<Title> TitleRepository { get; set; }
    }
}