using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Validations.UpdateOrCrestes;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;

namespace Fin.Test.Titles.Validations;

public class TitleInputWalletValidationTest : TestUtils.BaseTestWithContext
{
    private TitleInput GetValidInput(Guid walletId, DateTime date) => new()
    {
        Description = TestUtils.Strings[0],
        Value = TestUtils.Decimals[0],
        Date = date,
        WalletId = walletId,
        Type = TitleType.Income
    };

    private async Task<Wallet> CreateWalletInDatabase(
        Resources resources,
        Guid id,
        DateTime createdAt,
        bool inactivated = false)
    {
        var wallet = TestUtils.Wallets[0];
        wallet.Id = id;
        wallet.CreatedAt = createdAt; 

        if (inactivated)
            wallet.ToggleInactivated();

        await resources.WalletRepository.AddAsync(wallet, autoSave: true);
        return wallet;
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenWalletIsValid()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var walletCreationDate = TestUtils.UtcDateTimes[0];
        var titleDate = walletCreationDate.AddDays(1);
        var walletId = TestUtils.Guids[0];

        await CreateWalletInDatabase(resources, walletId, walletCreationDate, inactivated: false);
        var input = GetValidInput(walletId, titleDate);

        // Act
        var result = await service.ValidateAsync(input, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenWalletNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var nonExistentWalletId = TestUtils.Guids[9];
        var input = GetValidInput(nonExistentWalletId, TestUtils.UtcDateTimes[0]);

        // Act
        var result = await service.ValidateAsync(input, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.WalletNotFound);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenWalletIsInactive()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);
        
        var walletCreationDate = TestUtils.UtcDateTimes[0];
        var titleDate = walletCreationDate.AddDays(1);
        var walletId = TestUtils.Guids[0];

        await CreateWalletInDatabase(resources, walletId, walletCreationDate, inactivated: true);
        var input = GetValidInput(walletId, titleDate);

        // Act
        var result = await service.ValidateAsync(input, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.WalletInactive);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenTitleDateIsBeforeWalletCreation()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);
        
        var walletCreationDate = TestUtils.UtcDateTimes[1];
        var walletId = TestUtils.Guids[0];
        var wallet = await CreateWalletInDatabase(resources, walletId, walletCreationDate, inactivated: false);

        var titleDate = wallet.CreatedAt.AddDays(-1); // Before wallet creation
        var input = GetValidInput(walletId, titleDate);

        // Act
        var result = await service.ValidateAsync(input, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.TitleDateMustBeEqualOrAfterWalletCreation);
    }
    
    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenTitleDateIsSameAsWalletCreation()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);
        
        var walletCreationDate = TestUtils.UtcDateTimes[0];
        var titleDate = walletCreationDate;
        var walletId = TestUtils.Guids[0];

        await CreateWalletInDatabase(resources, walletId, walletCreationDate, inactivated: false);
        var input = GetValidInput(walletId, titleDate);

        // Act
        var result = await service.ValidateAsync(input, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    private TitleInputWalletValidation GetService(Resources resources)
    {
        return new TitleInputWalletValidation(resources.WalletRepository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            WalletRepository = GetRepository<Wallet>()
        };
    }

    private class Resources
    {
        public IRepository<Wallet> WalletRepository { get; set; }
    }
}