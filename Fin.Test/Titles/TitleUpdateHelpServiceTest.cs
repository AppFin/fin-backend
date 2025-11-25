using Fin.Application.Titles.Services;
using Fin.Application.Wallets.Services;
using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fin.Test.Titles;

public class TitleUpdateHelpServiceTest : TestUtils.BaseTestWithContext
{
    private readonly Mock<IWalletBalanceService> _balanceServiceMock;

    public TitleUpdateHelpServiceTest()
    {
        _balanceServiceMock = new Mock<IWalletBalanceService>();
    }

    #region UpdateTitleAndCategories

    [Fact]
    public async Task UpdateTitleAndCategories_ShouldUpdateTitleAndRemoveCategories()
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

        var titleCategory1 = TestUtils.TitleCategories[0];
        var titleCategory2 = TestUtils.TitleCategories[1];

        await resources.TitleCategoryRepository.AddAsync(titleCategory1, autoSave: true);
        await resources.TitleCategoryRepository.AddAsync(titleCategory2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid> { titleCategory1.Id, titleCategory2.Id }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);
        var previousBalance = title.PreviousBalance;

        // Create categories to remove
        var categoriesToRemove = title.TitleTitleCategories.Take(1).ToList();

        var updateInput = new TitleInput
        {
            Description = "Updated Description",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid> { titleCategory2.Id }
        };

        title.Update(updateInput, 1000m);
        title.SyncCategoriesAndReturnToRemove(updateInput.TitleCategoriesIds);

        var udpateContext = new UpdateTitleContext(
            wallet.Id,
            TestUtils.UtcDateTimes[0],
            previousBalance,
            categoriesToRemove,
            new List<TitlePerson>()
        );

        // Act
        await service.PerformUpdateTitle(title, udpateContext, CancellationToken.None);
        await Context.SaveChangesAsync();

        // Assert
        var updatedTitle = await resources.TitleRepository.AsNoTracking()
            .Include(t => t.TitleTitleCategories)
            .FirstAsync(t => t.Id == title.Id);

        updatedTitle.Description.Should().Be("Updated Description");
        updatedTitle.Value.Should().Be(600m);
        updatedTitle.TitleTitleCategories.Should().HaveCount(1);
    }

    #endregion

    #region PrepareUpdateContext

    [Fact]
    public async Task PrepareUpdateContext_ShouldReturnContext_WhenMustReprocess()
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
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var input = new TitleInput
        {
            Description = "Updated",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        };

        _balanceServiceMock
            .Setup(b => b.GetBalanceAt(
                wallet.Id,
                input.Date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1500m);

        // Act
        var context = await service.PrepareUpdateContext(title, input, mustReprocess: true, CancellationToken.None);

        // Assert
        context.Should().NotBeNull();
        context.PreviousWalletId.Should().Be(wallet.Id);
        context.PreviousDate.Should().Be(title.Date);
        context.PreviousBalance.Should().Be(title.PreviousBalance);
        context.CategoriesToRemove.Should().BeEmpty();
    }

    [Fact]
    public async Task PrepareUpdateContext_ShouldNotRecalculateBalance_WhenMustNotReprocess()
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
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var input = new TitleInput
        {
            Description = "Updated Description Only",
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var context = await service.PrepareUpdateContext(title, input, mustReprocess: false, CancellationToken.None);

        // Assert
        context.Should().NotBeNull();
        context.PreviousBalance.Should().Be(title.PreviousBalance);

        // Verify GetBalanceAt was NOT called
        _balanceServiceMock.Verify(
            b => b.GetBalanceAt(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region CalculatePreviousBalance

    [Fact]
    public async Task CalculatePreviousBalance_ShouldReturnBalanceMinusTitleValue_WhenSameWalletAndEarlierDate()
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
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var input = new TitleInput
        {
            Description = "Updated",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        };

        _balanceServiceMock
            .Setup(b => b.GetBalanceAt(
                wallet.Id,
                input.Date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1500m);

        // Act
        var previousBalance = await service.CalculatePreviousBalance(title, input, CancellationToken.None);

        // Assert
        // 1500 - 500 (title.EffectiveValue) = 1000
        previousBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task CalculatePreviousBalance_ShouldReturnRawBalance_WhenDifferentWallet()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet1 = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        var wallet2 = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[3],
            Color = TestUtils.Strings[4],
            Icon = TestUtils.Strings[5],
            InitialBalance = 2000m
        });
        await resources.WalletRepository.AddAsync(wallet1, autoSave: true);
        await resources.WalletRepository.AddAsync(wallet2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[6],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet1.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var input = new TitleInput
        {
            Description = "Updated",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet2.Id, // Different wallet
            TitleCategoriesIds = new List<Guid>()
        };

        _balanceServiceMock
            .Setup(b => b.GetBalanceAt(
                wallet2.Id,
                input.Date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2000m);

        // Act
        var previousBalance = await service.CalculatePreviousBalance(title, input, CancellationToken.None);

        // Assert
        // Should return raw balance without adjustment
        previousBalance.Should().Be(2000m);
    }

    [Fact]
    public async Task CalculatePreviousBalance_ShouldReturnRawBalance_WhenSameWalletButLaterDate()
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
            Date = TestUtils.UtcDateTimes[5], // Later date
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var input = new TitleInput
        {
            Description = "Updated",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0], // Earlier date
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        };

        _balanceServiceMock
            .Setup(b => b.GetBalanceAt(
                wallet.Id,
                input.Date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000m);

        // Act
        var previousBalance = await service.CalculatePreviousBalance(title, input, CancellationToken.None);

        // Assert
        // Should return raw balance (no adjustment because title.Date > input.Date)
        previousBalance.Should().Be(1000m);
    }

    #endregion

    #region GetTitlesForReprocessing

    [Fact]
    public async Task GetTitlesForReprocessing_ShouldReturnTitlesAfterDate()
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
        title1.Id = TestUtils.Guids[0];

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[2],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1500m);
        title2.Id = TestUtils.Guids[1];

        var title3 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 300m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[4],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1300m);
        title3.Id = TestUtils.Guids[2];

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);
        await resources.TitleRepository.AddAsync(title3, autoSave: true);

        // Act
        var titles = await service.GetTitlesForReprocessing(
            wallet.Id,
            TestUtils.UtcDateTimes[2],
            title1.Id,
            CancellationToken.None);

        // Assert
        titles.Should().HaveCount(2);
        titles.Should().Contain(t => t.Id == title2.Id);
        titles.Should().Contain(t => t.Id == title3.Id);
        titles.Should().NotContain(t => t.Id == title1.Id);
    }

    [Fact]
    public async Task GetTitlesForReprocessing_ShouldReturnEmpty_WhenNoTitlesAfterDate()
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

        await resources.TitleRepository.AddAsync(title1, autoSave: true);

        // Act
        var titles = await service.GetTitlesForReprocessing(
            wallet.Id,
            TestUtils.UtcDateTimes[5],
            title1.Id,
            CancellationToken.None);

        // Assert
        titles.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTitlesForReprocessing_ShouldFilterByWallet()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet1 = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        var wallet2 = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[3],
            Color = TestUtils.Strings[4],
            Icon = TestUtils.Strings[5],
            InitialBalance = 2000m
        });

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[6],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet1.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        title1.Id = TestUtils.Guids[0];

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[7],
            Value = 200m,
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[3],
            WalletId = wallet1.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1500m);
        title2.Id = TestUtils.Guids[1];

        var title3 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[8],
            Value = 300m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[3],
            WalletId = wallet2.Id, // Different wallet
            TitleCategoriesIds = new List<Guid>()
        }, 2000m);
        title3.Id = TestUtils.Guids[2];
        wallet1.Titles.Add(title1);
        wallet1.Titles.Add(title2);
        wallet2.Titles.Add(title3);

        await resources.WalletRepository.AddRangeAsync([wallet1, wallet2], autoSave: true);

        // Act
        var titles = await service.GetTitlesForReprocessing(
            wallet1.Id,
            TestUtils.UtcDateTimes[0],
            title1.Id,
            CancellationToken.None);

        // Assert
        titles.Should().HaveCount(1);
        titles.Should().Contain(t => t.Id == title2.Id);
        titles.Should().NotContain(t => t.Id == title3.Id);
    }

    #endregion

    #region ReprocessAffectedWallets

    [Fact]
    public async Task ReprocessAffectedWallets_ShouldReprocessBothWallets_WhenWalletChanged()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet1 = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2],
            InitialBalance = 1000m
        });
        var wallet2 = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[3],
            Color = TestUtils.Strings[4],
            Icon = TestUtils.Strings[5],
            InitialBalance = 2000m
        });
        await resources.WalletRepository.AddAsync(wallet1, autoSave: true);
        await resources.WalletRepository.AddAsync(wallet2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[6],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet2.Id, // Changed to wallet2
            TitleCategoriesIds = new List<Guid>()
        }, 2000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var context = new UpdateTitleContext(
            PreviousWalletId: wallet1.Id, // Was in wallet1
            PreviousDate: TestUtils.UtcDateTimes[0],
            PreviousBalance: 1000m,
            CategoriesToRemove: new List<TitleTitleCategory>(),
            PeopleToRemove: new List<TitlePerson>()
        );

        _balanceServiceMock
            .Setup(b => b.ReprocessBalance(
                It.IsAny<List<Title>>(),
                It.IsAny<decimal>(),
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.ReprocessAffectedWallets(title, context, autoSave: false, CancellationToken.None);

        // Assert
        // Should reprocess both wallets (current and previous)
        _balanceServiceMock.Verify(
            b => b.ReprocessBalance(
                It.IsAny<List<Title>>(),
                It.IsAny<decimal>(),
                false,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ReprocessAffectedWallets_ShouldReprocessOnlyCurrentWallet_WhenWalletNotChanged()
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
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var context = new UpdateTitleContext(
            PreviousWalletId: wallet.Id, // Same wallet
            PreviousDate: TestUtils.UtcDateTimes[0],
            PreviousBalance: 1000m,
            CategoriesToRemove: new List<TitleTitleCategory>(),
            PeopleToRemove: new List<TitlePerson>()
        );

        _balanceServiceMock
            .Setup(b => b.ReprocessBalance(
                It.IsAny<List<Title>>(),
                It.IsAny<decimal>(),
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.ReprocessAffectedWallets(title, context, autoSave: false, CancellationToken.None);

        // Assert
        // Should reprocess only current wallet
        _balanceServiceMock.Verify(
            b => b.ReprocessBalance(
                It.IsAny<List<Title>>(),
                It.IsAny<decimal>(),
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateTitleAndPeople

    [Fact]
    public async Task UpdateTitleAndPeople_ShouldUpdateTitleAndRemovePeople()
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

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[3] });
        var person2 = new Person(new PersonInput { Name = TestUtils.Strings[4] });
        await resources.PersonRepository.AddAsync(person1, autoSave: true);
        await resources.PersonRepository.AddAsync(person2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 50m },
                new() { PersonId = person2.Id, Percentage = 50m }
            }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);
        var previousBalance = title.PreviousBalance;

        // Create people to remove
        var peopleToRemove = title.TitlePeople.Take(1).ToList();

        var updateInput = new TitleInput
        {
            Description = "Updated Description",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person2.Id, Percentage = 100m }
            }
        };

        title.Update(updateInput, 1000m);
        title.SyncPeopleAndReturnToRemove(updateInput.TitlePeople);

        var updateContext = new UpdateTitleContext(
            wallet.Id,
            TestUtils.UtcDateTimes[0],
            previousBalance,
            new List<TitleTitleCategory>(),
            peopleToRemove
        );

        // Act
        await service.PerformUpdateTitle(title, updateContext, CancellationToken.None);
        await Context.SaveChangesAsync();

        // Assert
        var updatedTitle = await resources.TitleRepository.AsNoTracking()
            .Include(t => t.TitlePeople)
            .FirstAsync(t => t.Id == title.Id);

        updatedTitle.Description.Should().Be("Updated Description");
        updatedTitle.Value.Should().Be(600m);
        updatedTitle.TitlePeople.Should().HaveCount(1);
        updatedTitle.TitlePeople.First().PersonId.Should().Be(person2.Id);
        updatedTitle.TitlePeople.First().Percentage.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateTitleAndPeople_ShouldUpdateTitleAndAddPeople()
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

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[3] });
        var person2 = new Person(new PersonInput { Name = TestUtils.Strings[4] });
        await resources.PersonRepository.AddAsync(person1, autoSave: true);
        await resources.PersonRepository.AddAsync(person2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 100m }
            }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);
        var previousBalance = title.PreviousBalance;

        var updateInput = new TitleInput
        {
            Description = "Updated Description",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 50m },
                new() { PersonId = person2.Id, Percentage = 50m }
            }
        };

        title.Update(updateInput, 1000m);
        var peopleToRemove = title.SyncPeopleAndReturnToRemove(updateInput.TitlePeople);

        var updateContext = new UpdateTitleContext(
            wallet.Id,
            TestUtils.UtcDateTimes[0],
            previousBalance,
            new List<TitleTitleCategory>(),
            peopleToRemove
        );

        // Act
        await service.PerformUpdateTitle(title, updateContext, CancellationToken.None);
        await Context.SaveChangesAsync();

        // Assert
        var updatedTitle = await resources.TitleRepository.AsNoTracking()
            .Include(t => t.TitlePeople)
            .FirstAsync(t => t.Id == title.Id);

        updatedTitle.TitlePeople.Should().HaveCount(2);
        updatedTitle.TitlePeople.Select(tp => tp.PersonId).Should().Contain(person1.Id);
        updatedTitle.TitlePeople.Select(tp => tp.PersonId).Should().Contain(person2.Id);
    }

    [Fact]
    public async Task UpdateTitleAndPeople_ShouldUpdatePersonPercentage()
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

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[3] });
        await resources.PersonRepository.AddAsync(person1, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 50m }
            }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var updateInput = new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 75m }
            }
        };

        title.Update(updateInput, 1000m);
        var peopleToRemove = title.SyncPeopleAndReturnToRemove(updateInput.TitlePeople);

        var updateContext = new UpdateTitleContext(
            wallet.Id,
            TestUtils.UtcDateTimes[0],
            1000m,
            new List<TitleTitleCategory>(),
            peopleToRemove
        );

        // Act
        await service.PerformUpdateTitle(title, updateContext, CancellationToken.None);
        await Context.SaveChangesAsync();

        // Assert
        var updatedTitle = await resources.TitleRepository.AsNoTracking()
            .Include(t => t.TitlePeople)
            .FirstAsync(t => t.Id == title.Id);

        updatedTitle.TitlePeople.Should().HaveCount(1);
        updatedTitle.TitlePeople.First().PersonId.Should().Be(person1.Id);
        updatedTitle.TitlePeople.First().Percentage.Should().Be(75m);
    }

    [Fact]
    public async Task UpdateTitleAndPeople_ShouldRemoveAllPeople()
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

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[3] });
        var person2 = new Person(new PersonInput { Name = TestUtils.Strings[4] });
        await resources.PersonRepository.AddAsync(person1, autoSave: true);
        await resources.PersonRepository.AddAsync(person2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 50m },
                new() { PersonId = person2.Id, Percentage = 50m }
            }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var updateInput = new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>()
        };

        title.Update(updateInput, 1000m);
        var peopleToRemove = title.SyncPeopleAndReturnToRemove(updateInput.TitlePeople);

        var updateContext = new UpdateTitleContext(
            wallet.Id,
            TestUtils.UtcDateTimes[0],
            1000m,
            new List<TitleTitleCategory>(),
            peopleToRemove
        );

        // Act
        await service.PerformUpdateTitle(title, updateContext, CancellationToken.None);
        await Context.SaveChangesAsync();

        // Assert
        var updatedTitle = await resources.TitleRepository.AsNoTracking()
            .Include(t => t.TitlePeople)
            .FirstAsync(t => t.Id == title.Id);

        updatedTitle.TitlePeople.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTitleAndPeople_ShouldHandleCategoriesAndPeopleSimultaneously()
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

        var category1 = TestUtils.TitleCategories[0];
        var category2 = TestUtils.TitleCategories[1];
        await resources.TitleCategoryRepository.AddAsync(category1, autoSave: true);
        await resources.TitleCategoryRepository.AddAsync(category2, autoSave: true);

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[3] });
        var person2 = new Person(new PersonInput { Name = TestUtils.Strings[4] });
        await resources.PersonRepository.AddAsync(person1, autoSave: true);
        await resources.PersonRepository.AddAsync(person2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid> { category1.Id, category2.Id },
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 50m },
                new() { PersonId = person2.Id, Percentage = 50m }
            }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var updateInput = new TitleInput
        {
            Description = "Updated",
            Value = 600m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid> { category2.Id },
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 100m }
            }
        };

        title.Update(updateInput, 1000m);
        var categoriesToRemove = title.SyncCategoriesAndReturnToRemove(updateInput.TitleCategoriesIds);
        var peopleToRemove = title.SyncPeopleAndReturnToRemove(updateInput.TitlePeople);

        var updateContext = new UpdateTitleContext(
            wallet.Id,
            TestUtils.UtcDateTimes[0],
            1000m,
            categoriesToRemove,
            peopleToRemove
        );

        // Act
        await service.PerformUpdateTitle(title, updateContext, CancellationToken.None);
        await Context.SaveChangesAsync();

        // Assert
        var updatedTitle = await resources.TitleRepository.AsNoTracking()
            .Include(t => t.TitleTitleCategories)
            .Include(t => t.TitlePeople)
            .FirstAsync(t => t.Id == title.Id);

        updatedTitle.Description.Should().Be("Updated");
        updatedTitle.Value.Should().Be(600m);
        updatedTitle.TitleTitleCategories.Should().HaveCount(1);
        updatedTitle.TitleTitleCategories.First().TitleCategoryId.Should().Be(category2.Id);
        updatedTitle.TitlePeople.Should().HaveCount(1);
        updatedTitle.TitlePeople.First().PersonId.Should().Be(person1.Id);
        updatedTitle.TitlePeople.First().Percentage.Should().Be(100m);
    }

    #endregion

    #region PrepareUpdateContext - People

    [Fact]
    public async Task PrepareUpdateContext_ShouldIncludePeopleToRemove()
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

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[3] });
        var person2 = new Person(new PersonInput { Name = TestUtils.Strings[4] });
        await resources.PersonRepository.AddAsync(person1, autoSave: true);
        await resources.PersonRepository.AddAsync(person2, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 50m },
                new() { PersonId = person2.Id, Percentage = 50m }
            }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var input = new TitleInput
        {
            Description = "Updated",
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 100m }
            }
        };

        // Act
        var context = await service.PrepareUpdateContext(title, input, mustReprocess: false, CancellationToken.None);

        // Assert
        context.Should().NotBeNull();
        context.PeopleToRemove.Should().HaveCount(1);
        context.CategoriesToRemove.Should().BeEmpty();
    }

    [Fact]
    public async Task PrepareUpdateContext_ShouldReturnEmptyPeopleList_WhenNoPeopleRemoved()
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

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[3] });
        await resources.PersonRepository.AddAsync(person1, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[5],
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 50m }
            }
        }, 1000m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var input = new TitleInput
        {
            Description = "Updated",
            Value = 500m,
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = new List<TitlePersonInput>
            {
                new() { PersonId = person1.Id, Percentage = 75m } // Same person, different percentage
            }
        };

        // Act
        var context = await service.PrepareUpdateContext(title, input, mustReprocess: false, CancellationToken.None);

        // Assert
        context.Should().NotBeNull();
        context.PeopleToRemove.Should().HaveCount(0);
    }

    #endregion

    private TitleUpdateHelpService GetService(Resources resources)
    {
        return new TitleUpdateHelpService(
            resources.TitleRepository,
            resources.TitleTitleCategoryRepository,
            resources.TitlePersonsRepository,
            _balanceServiceMock.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleRepository = GetRepository<Title>(),
            TitleTitleCategoryRepository = GetRepository<TitleTitleCategory>(),
            TitleCategoryRepository = GetRepository<TitleCategory>(),
            WalletRepository = GetRepository<Wallet>(),
            TitlePersonsRepository = GetRepository<TitlePerson>(),
            PersonRepository = GetRepository<Person>() 
        };
    }

    private class Resources
    {
        public IRepository<Title> TitleRepository { get; set; }
        public IRepository<TitleTitleCategory> TitleTitleCategoryRepository { get; set; }
        public IRepository<TitleCategory> TitleCategoryRepository { get; set; }
        public IRepository<TitlePerson> TitlePersonsRepository { get; set; }
        public IRepository<Wallet> WalletRepository { get; set; }
        public IRepository<Person> PersonRepository { get; set; }
    }
}