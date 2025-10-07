using Fin.Application.Wallets.Enums;
using Fin.Application.Wallets.Services;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;

namespace Fin.Test.Wallets.Services;

public class WalletValidationServiceTest : TestUtils.BaseTestWithContext
{
    private WalletValidationService GetService(Resources resources)
    {
        return new WalletValidationService(resources.WalletRepository);
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

    #region ValidateToggleInactive

    [Fact]
    public async Task ValidateToggleInactive_ShouldReturnSuccess_WhenWalletExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);

        // Act
        var result = await service.ValidateToggleInactive(wallet.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToggleInactive_ShouldReturnFailure_WhenWalletNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];

        // Act
        var result = await service.ValidateToggleInactive(nonExistentId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletToogleInactiveErrorCode.WalletNotFound);
        result.Message.Should().Be("Wallet not found to toogle inactive.");
    }

    #endregion

    #region ValidateDelete

    [Fact]
    public async Task ValidateDelete_ShouldReturnSuccess_WhenWalletExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);

        // Act
        var result = await service.ValidateDelete(wallet.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDelete_ShouldReturnFailure_WhenWalletNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];

        // Act
        var result = await service.ValidateDelete(nonExistentId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletDeleteErrorCode.WalletNotFound);
        result.Message.Should().Be("Wallet not found to delete.");
    }

    #endregion
    
    #region ValidateInput (Create and Update)
    
    private WalletInput GetValidInput() => new()
    {
        Name = "New Wallet",
        Color = "#FFFFFF",
        Icon = "fa-icon",
        InitialBalance = 0m
    };

    [Fact]
    public async Task ValidateInput_Create_ShouldReturnSuccess_WhenValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateInput_Update_ShouldReturnSuccess_WhenValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var wallet = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 100 });
        await resources.WalletRepository.AddAsync(wallet, true);
        var input = GetValidInput();

        // Act
        var result = await service.ValidateInput<bool>(input, wallet.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateInput_Update_ShouldReturnFailure_WhenWalletNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];
        var input = GetValidInput();

        // Act
        var result = await service.ValidateInput<bool>(input, nonExistentId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.WalletNotFound);
        result.Message.Should().Be("Wallet not found to edit.");
    }
    
    // --- Testes de Campos Obrigatórios e Tamanho ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameIsRequired(string name)
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Name = name;

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameIsRequired);
        result.Message.Should().Be("Name is required.");
    }
    
    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameTooLong()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Name = new string('A', 101); // Max 100

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameTooLong);
        result.Message.Should().Be("Name is too long. Max 100 characters.");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateInput_ShouldReturnFailure_WhenColorIsRequired(string color)
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Color = color;

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.ColorIsRequired);
        result.Message.Should().Be("Color is required.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenColorTooLong()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Color = new string('A', 21); // Max 20

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.ColorTooLong);
        result.Message.Should().Be("Color is too long. Max 20 characters.");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateInput_ShouldReturnFailure_WhenIconIsRequired(string icon)
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Icon = icon;

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.IconIsRequired);
        result.Message.Should().Be("Icon is required.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenIconTooLong()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Icon = new string('A', 21); // Max 20

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.IconTooLong);
        result.Message.Should().Be("Icon is too long. Max 20 characters.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameAlreadyInUseOnCreate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var existingName = TestUtils.Strings[0];
        
        // Categoria existente
        await resources.WalletRepository.AddAsync(new Wallet(new WalletInput { Name = existingName, Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 0m }), true);

        var input = GetValidInput();
        input.Name = existingName;

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameAlreadyInUse);
        result.Message.Should().Be("Name is already in use.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnSuccess_WhenNameAlreadyInUseBySelfOnUpdate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var existingName = TestUtils.Strings[0];
        
        var wallet = new Wallet(new WalletInput { Name = existingName, Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 0m });
        await resources.WalletRepository.AddAsync(wallet, true);

        var input = GetValidInput();
        input.Name = existingName;

        // Act
        var result = await service.ValidateInput<bool>(input, wallet.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameAlreadyInUseByAnotherWalletOnUpdate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        var walletA = new Wallet(new WalletInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], InitialBalance = 0m });
        await resources.WalletRepository.AddAsync(walletA, true);

        var walletB = new Wallet(new WalletInput { Name = TestUtils.Strings[3], Color = TestUtils.Strings[4], Icon = TestUtils.Strings[5], InitialBalance = 0m });
        await resources.WalletRepository.AddAsync(walletB, true);

        var input = GetValidInput();
        input.Name = walletA.Name;

        // Act
        var result = await service.ValidateInput<bool>(input, walletB.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameAlreadyInUse);
        result.Message.Should().Be("Name is already in use.");
    }

    #endregion
}