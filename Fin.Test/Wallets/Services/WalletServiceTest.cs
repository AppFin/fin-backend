using Fin.Application.Globals.Dtos;
using Fin.Application.Wallets.Dtos;
using Fin.Application.Wallets.Enums;
using Fin.Application.Wallets.Services;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fin.Test.Wallets.Services;

public class WalletServiceTest : TestUtils.BaseTestWithContext
{
    private readonly Mock<IWalletValidationService> _validationServiceMock;

    public WalletServiceTest()
    {
        _validationServiceMock = new Mock<IWalletValidationService>();
    }

    #region Get

    [Fact]
    public async Task Get_ShouldReturnWallet_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);

        // Act
        var result = await service.Get(wallet.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(wallet.Id);
        result.Name.Should().Be(wallet.Name);
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

        await resources.WalletRepository.AddAsync(new Wallet(new WalletInput { Name = "C", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], InitialBalance = 10 }), true);
        await resources.WalletRepository.AddAsync(new Wallet(new WalletInput { Name = "A", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], InitialBalance = 10 }), true);
        await resources.WalletRepository.AddAsync(new Wallet(new WalletInput { Name = "B", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], InitialBalance = 10 }), true);

        var input = new WalletGetListInput { MaxResultCount = 2, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        // Default sort is Inactivated (false first) then Name (asc).
        result.Items.First().Name.Should().Be("A");
        result.Items.Last().Name.Should().Be("B");
    }

    [Fact]
    public async Task GetList_ShouldFilterByInactivatedTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await resources.WalletRepository.AddAsync(new Wallet(new WalletInput { Name = "Active1", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], InitialBalance = 10 }), true);
        var inactive = new Wallet(new WalletInput { Name = "Inactive1", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], InitialBalance = 10 });
        inactive.ToggleInactivated();
        await resources.WalletRepository.AddAsync(inactive, true);
        var inactive2 = new Wallet(new WalletInput { Name = "Inactive2", Color = TestUtils.Strings[1], Icon = TestUtils.Strings[3], InitialBalance = 10 });
        inactive2.ToggleInactivated();
        await resources.WalletRepository.AddAsync(inactive2, true);

        var input = new WalletGetListInput { Inactivated = true, MaxResultCount = 10, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("Inactive1");
        result.Items.Last().Name.Should().Be("Inactive2");
    }

    #endregion
    
    #region Create

    [Fact]
    public async Task Create_ShouldReturnSuccessAndWallet_WhenInputIsValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 50.5m };
        
        var successValidation = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateInput<WalletOutput>(input, null)).ReturnsAsync(successValidation);

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var dbWallet = await resources.WalletRepository.Query(false).FirstOrDefaultAsync(a => a.Id == result.Data.Id);
        dbWallet.Should().NotBeNull();
        dbWallet.Name.Should().Be(input.Name);
        dbWallet.InitialBalance.Should().Be(input.InitialBalance);
        dbWallet.CurrentBalance.Should().Be(input.InitialBalance);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 50.5m };
        
        var failureValidation = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>
        { 
            Success = false, 
            ErrorCode = WalletCreateOrUpdateErrorCode.NameIsRequired,
            Message = "Name is required."
        };
        _validationServiceMock.Setup(v => v.ValidateInput<WalletOutput>(input, null)).ReturnsAsync(failureValidation);

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameIsRequired);
        result.Data.Should().BeNull();
        
        (await resources.WalletRepository.Query(false).CountAsync()).Should().Be(0); // Ensure no addition to DB
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnSuccess_WhenInputIsValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);

        var input = new WalletInput { Name = TestUtils.Strings[4], Color = TestUtils.Strings[5], Icon = TestUtils.Strings[6], InitialBalance = 200m };
        
        var successValidation = new ValidationResultDto<bool, WalletCreateOrUpdateErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateInput<bool>(input, wallet.Id)).ReturnsAsync(successValidation);

        // Act
        var result = await service.Update(wallet.Id, input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var dbWallet = await resources.WalletRepository.Query(false).FirstAsync(a => a.Id == wallet.Id);
        dbWallet.Name.Should().Be(input.Name);
        dbWallet.InitialBalance.Should().Be(input.InitialBalance);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);

        var input = new WalletInput { Name = TestUtils.Strings[4], Color = TestUtils.Strings[5], Icon = TestUtils.Strings[6], InitialBalance = 200m };

        var failureValidation = new ValidationResultDto<bool, WalletCreateOrUpdateErrorCode>
        { 
            Success = false, 
            ErrorCode = WalletCreateOrUpdateErrorCode.WalletNotFound,
            Message = "Wallet not found."
        };
        _validationServiceMock.Setup(v => v.ValidateInput<bool>(input, wallet.Id)).ReturnsAsync(failureValidation);

        // Act
        var result = await service.Update(wallet.Id, input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.WalletNotFound);
        result.Data.Should().BeFalse();

        var dbWallet = await resources.WalletRepository.Query(false).FirstAsync(a => a.Id == wallet.Id);
        dbWallet.Name.Should().Be(TestUtils.Strings[0]); // Ensure no update happened
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);
        
        var successValidation = new ValidationResultDto<bool, WalletDeleteErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateDelete(wallet.Id)).ReturnsAsync(successValidation);

        // Act
        var result = await service.Delete(wallet.Id, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        (await resources.WalletRepository.Query(false).FirstOrDefaultAsync(a => a.Id == wallet.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);

        var failureValidation = new ValidationResultDto<bool, WalletDeleteErrorCode>
        { 
            Success = false, 
            ErrorCode = WalletDeleteErrorCode.WalletInUseByCreditCardsAndTitle,
            Message = "Cannot delete wallet with transactions."
        };
        _validationServiceMock.Setup(v => v.ValidateDelete(wallet.Id)).ReturnsAsync(failureValidation);

        // Act
        var result = await service.Delete(wallet.Id, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletDeleteErrorCode.WalletInUseByCreditCardsAndTitle);
        (await resources.WalletRepository.Query(false).FirstOrDefaultAsync(a => a.Id == wallet.Id)).Should().NotBeNull(); // Ensure no deletion
    }

    #endregion

    #region ToggleInactive

    [Fact]
    public async Task ToggleInactive_ShouldReturnSuccess_WhenValidAndDeactivate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);
        wallet.Inactivated.Should().BeFalse();
        
        var successValidation = new ValidationResultDto<bool, WalletToggleInactiveErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateToggleInactive(wallet.Id)).ReturnsAsync(successValidation);

        // Act
        var result = await service.ToggleInactive(wallet.Id, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        var dbWallet = await resources.WalletRepository.Query(false).FirstAsync(a => a.Id == wallet.Id);
        dbWallet.Inactivated.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnSuccess_WhenValidAndReactivate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        wallet.ToggleInactivated();
        await resources.WalletRepository.AddAsync(wallet, true);
        wallet.Inactivated.Should().BeTrue();
        
        var successValidation = new ValidationResultDto<bool, WalletToggleInactiveErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateToggleInactive(wallet.Id)).ReturnsAsync(successValidation);

        // Act
        var result = await service.ToggleInactive(wallet.Id, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        var dbWallet = await resources.WalletRepository.Query(false).FirstAsync(a => a.Id == wallet.Id);
        dbWallet.Inactivated.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);
        wallet.Inactivated.Should().BeFalse();

        var failureValidation = new ValidationResultDto<bool, WalletToggleInactiveErrorCode>
        { 
            Success = false, 
            ErrorCode = WalletToggleInactiveErrorCode.WalletNotFound,
            Message = "Wallet not found."
        };
        _validationServiceMock.Setup(v => v.ValidateToggleInactive(wallet.Id)).ReturnsAsync(failureValidation);

        // Act
        var result = await service.ToggleInactive(wallet.Id, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletToggleInactiveErrorCode.WalletNotFound);
        var dbWallet = await resources.WalletRepository.Query(false).FirstAsync(a => a.Id == wallet.Id);
        dbWallet.Inactivated.Should().BeFalse(); // Ensure no status change
    }

    #endregion

    private WalletService GetService(Resources resources)
    {
        return new WalletService(resources.WalletRepository, _validationServiceMock.Object);
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