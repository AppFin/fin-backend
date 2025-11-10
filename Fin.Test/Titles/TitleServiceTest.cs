using Fin.Application.Titles.Dtos;
using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Services;
using Fin.Application.Wallets.Services;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fin.Test.Titles;

public class TitleServiceTest : TestUtils.BaseTestWithContext
{
    private readonly Mock<ITitleUpdateHelpService> _updateHelpServiceMock;
    private readonly Mock<IWalletBalanceService> _balanceServiceMock;
    private readonly Mock<IValidationPipelineOrchestrator> _validationMock;

    public TitleServiceTest()
    {
        _updateHelpServiceMock = new Mock<ITitleUpdateHelpService>();
        _balanceServiceMock = new Mock<IWalletBalanceService>();
        _validationMock = new Mock<IValidationPipelineOrchestrator>();
    }

    #region Get

    [Fact]
    public async Task Get_ShouldReturnTitle_WhenExists()
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
            InitialBalance = TestUtils.Decimals[0]
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var titleInput = new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        };
        var title = new Title(titleInput, 0m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        // Act
        var result = await service.Get(title.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(title.Id);
        result.Description.Should().Be(titleInput.Description);
        result.Value.Should().Be(titleInput.Value);
        result.Type.Should().Be(titleInput.Type);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenDoesNotExist()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];

        // Act
        var result = await service.Get(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult()
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
            InitialBalance = TestUtils.Decimals[0]
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = "A - First",
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        var title2 = new Title(new TitleInput
        {
            Description = "B - Second",
            Value = TestUtils.Decimals[2],
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);

        var input = new TitleGetListInput
        {
            SkipCount = 0,
            MaxResultCount = 10
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetList_ShouldFilterByType()
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
            InitialBalance = TestUtils.Decimals[0]
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var incomeTitle = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        var expenseTitle = new Title(new TitleInput
        {
            Description = TestUtils.Strings[4],
            Value = TestUtils.Decimals[2],
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        await resources.TitleRepository.AddAsync(incomeTitle, autoSave: true);
        await resources.TitleRepository.AddAsync(expenseTitle, autoSave: true);

        var input = new TitleGetListInput
        {
            Type = TitleType.Income,
            SkipCount = 0,
            MaxResultCount = 10
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Type.Should().Be(TitleType.Income);
    }

    [Fact]
    public async Task GetList_ShouldFilterByWalletIds()
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
            InitialBalance = TestUtils.Decimals[0]
        });
        var wallet2 = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[3],
            Color = TestUtils.Strings[4],
            Icon = TestUtils.Strings[5],
            InitialBalance = TestUtils.Decimals[1]
        });

        await resources.WalletRepository.AddAsync(wallet1, autoSave: true);
        await resources.WalletRepository.AddAsync(wallet2, autoSave: true);

        var title1 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[6],
            Value = TestUtils.Decimals[2],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet1.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        var title2 = new Title(new TitleInput
        {
            Description = TestUtils.Strings[7],
            Value = TestUtils.Decimals[3],
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet2.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);

        await resources.TitleRepository.AddAsync(title1, autoSave: true);
        await resources.TitleRepository.AddAsync(title2, autoSave: true);

        var input = new TitleGetListInput
        {
            WalletIds = new List<Guid> { wallet1.Id },
            SkipCount = 0,
            MaxResultCount = 10
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().WalletId.Should().Be(wallet1.Id);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnSuccess_WhenInputIsValid()
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
            InitialBalance = TestUtils.Decimals[0]
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var input = new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        };

        var successValidation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>
        {
            Code = null
        };

        _validationMock
            .Setup(v => v.Validate<TitleInput, TitleCreateOrUpdateErrorCode>(
                input,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successValidation);

        _balanceServiceMock
            .Setup(b => b.GetBalanceAt(
                wallet.Id,
                input.Date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(100m);

        _balanceServiceMock
            .Setup(b => b.ReprocessBalanceFrom(
                It.IsAny<Title>(),
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.Create(input, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Description.Should().Be(input.Description);
        result.Data.Value.Should().Be(input.Value);
        result.Data.Type.Should().Be(input.Type);

        var dbTitle = await resources.TitleRepository.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == result.Data.Id);

        dbTitle.Should().NotBeNull();
        dbTitle.Description.Should().Be(input.Description);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        var failureValidation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>
        {
            Code = TitleCreateOrUpdateErrorCode.WalletNotFound
        };

        _validationMock
            .Setup(v => v.Validate<TitleInput, TitleCreateOrUpdateErrorCode>(
                input,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureValidation);

        // Act
        var result = await service.Create(input, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCreateOrUpdateErrorCode.WalletNotFound);
        result.Data.Should().BeNull();

        var count = await resources.TitleRepository.AsNoTracking().CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnSuccess_WhenInputIsValid()
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
            InitialBalance = TestUtils.Decimals[0]
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var updateInput = new TitleInput
        {
            Description = "Updated Description",
            Value = TestUtils.Decimals[2],
            Type = TitleType.Expense,
            Date = TestUtils.UtcDateTimes[1],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        };

        var successValidation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>
        {
            Code = null
        };

        _validationMock
            .Setup(v => v.Validate<TitleInput, TitleCreateOrUpdateErrorCode>(
                updateInput,
                title.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successValidation);

        var context = new UpdateTitleContext(
            PreviousWalletId: wallet.Id,
            PreviousDate: title.Date,
            PreviousBalance: title.PreviousBalance,
            CategoriesToRemove: new List<TitleTitleCategory>()
        );

        _updateHelpServiceMock
            .Setup(u => u.PrepareUpdateContext(
                It.IsAny<Title>(),
                updateInput,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        _updateHelpServiceMock
            .Setup(u => u.PerformUpdateTitle(
                It.IsAny<Title>(),
                updateInput,
                It.IsAny<List<TitleTitleCategory>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _updateHelpServiceMock
            .Setup(u => u.ReprocessAffectedWallets(
                It.IsAny<Title>(),
                context,
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.Update(title.Id, updateInput, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var titleId = TestUtils.Guids[0];
        var input = new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[1],
            TitleCategoriesIds = new List<Guid>()
        };

        var failureValidation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>
        {
            Code = TitleCreateOrUpdateErrorCode.TitleNotFound
        };

        _validationMock
            .Setup(v => v.Validate<TitleInput, TitleCreateOrUpdateErrorCode>(
                input,
                titleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureValidation);

        // Act
        var result = await service.Update(titleId, input, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleCreateOrUpdateErrorCode.TitleNotFound);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenTitleExists()
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
            InitialBalance = TestUtils.Decimals[0]
        });
        await resources.WalletRepository.AddAsync(wallet, autoSave: true);

        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[3],
            Value = TestUtils.Decimals[1],
            Type = TitleType.Income,
            Date = TestUtils.UtcDateTimes[0],
            WalletId = wallet.Id,
            TitleCategoriesIds = new List<Guid>()
        }, 0m);
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        var successValidation = new ValidationPipelineOutput<TitleDeleteErrorCode>
        {
            Code = null
        };

        _validationMock
            .Setup(v => v.Validate<Guid, TitleDeleteErrorCode>(
                title.Id,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successValidation);

        _updateHelpServiceMock
            .Setup(u => u.GetTitlesForReprocessing(
                wallet.Id,
                title.Date,
                title.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Title>());

        _balanceServiceMock
            .Setup(b => b.ReprocessBalance(
                It.IsAny<List<Title>>(),
                It.IsAny<decimal>(),
                false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.Delete(title.Id, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var dbTitle = await resources.TitleRepository.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == title.Id);
        dbTitle.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var titleId = TestUtils.Guids[0];

        var failureValidation = new ValidationPipelineOutput<TitleDeleteErrorCode>
        {
            Code = TitleDeleteErrorCode.TitleNotFound
        };

        _validationMock
            .Setup(v => v.Validate<Guid, TitleDeleteErrorCode>(
                titleId,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureValidation);

        // Act
        var result = await service.Delete(titleId, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(TitleDeleteErrorCode.TitleNotFound);
    }

    #endregion

    private TitleService GetService(Resources resources)
    {
        return new TitleService(
            resources.TitleRepository,
            _updateHelpServiceMock.Object,
            _balanceServiceMock.Object,
            UnitOfWork,
            _validationMock.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleRepository = GetRepository<Title>(),
            WalletRepository = GetRepository<Wallet>()
        };
    }

    private class Resources
    {
        public IRepository<Title> TitleRepository { get; set; }
        public IRepository<Wallet> WalletRepository { get; set; }
    }
}