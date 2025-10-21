using Fin.Application.CardBrands;
using Fin.Application.FinancialInstitutions;
using Fin.Application.Globals.Dtos;
using Fin.Application.CreditCards.Enums;
using Fin.Application.CreditCards.Services;
using Fin.Application.Wallets.Services;
using Fin.Domain.CreditCards.Dtos;
using Fin.Domain.CreditCards.Entities;
using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.Wallets.Dtos;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.CreditCards.Services;

public class CreditCardValidationServiceTest : TestUtils.BaseTestWithContext
{
    private CreditCardValidationService GetService(Resources resources)
    {
        return new CreditCardValidationService(
            resources.CreditCardRepository,
            resources.FakeFinancialInstitution.Object,
            resources.FakeWalletService.Object,
            resources.FakeCardBrandService.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            CreditCardRepository = GetRepository<CreditCard>(),
            FakeFinancialInstitution = new Mock<IFinancialInstitutionService>(),
            FakeWalletService = new Mock<IWalletService>(),
            FakeCardBrandService = new Mock<ICardBrandService>()
        };
    }

    private class Resources
    {
        public IRepository<CreditCard> CreditCardRepository { get; set; }
        public Mock<IFinancialInstitutionService> FakeFinancialInstitution { get; set; }
        public Mock<IWalletService> FakeWalletService { get; set; }
        public Mock<ICardBrandService> FakeCardBrandService { get; set; }
    }

    #region ValidateToggleInactive

    [Fact]
    public async Task ValidateToggleInactive_ShouldReturnSuccess_WhenCreditCardExists()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = new CreditCardInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], CardBrandId = TestUtils.Guids[0], DebitWalletId = TestUtils.Guids[1], FinancialInstitutionId = TestUtils.Guids[2] };
        var creditCard = new CreditCard(input);
        creditCard.CardBrand = TestUtils.CardBrands[0];
        creditCard.FinancialInstitution = TestUtils.FinancialInstitutions[0];
        creditCard.DebitWallet = TestUtils.Wallets[0];
        creditCard.DebitWallet.FinancialInstitution = creditCard.FinancialInstitution;
        
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var result = await service.ValidateToggleInactive(creditCard.Id);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToggleInactive_ShouldReturnFailure_WhenCreditCardNotFound()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];

        var result = await service.ValidateToggleInactive(nonExistentId);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardToggleInactiveErrorCode.CreditCardNotFound);
        result.Message.Should().Be("CreditCard not found to toggle inactive.");
    }

    #endregion

    #region ValidateDelete

    [Fact]
    public async Task ValidateDelete_ShouldReturnSuccess_WhenCreditCardExists()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = new CreditCardInput { Name = TestUtils.Strings[0], Color = TestUtils.Strings[1], Icon = TestUtils.Strings[2], CardBrandId = TestUtils.Guids[0], DebitWalletId = TestUtils.Guids[1], FinancialInstitutionId = TestUtils.Guids[2] };
        var creditCard = new CreditCard(input);
        creditCard.CardBrand = TestUtils.CardBrands[0];
        creditCard.FinancialInstitution = TestUtils.FinancialInstitutions[0];
        creditCard.DebitWallet = TestUtils.Wallets[0];
        creditCard.DebitWallet.FinancialInstitution = creditCard.FinancialInstitution;
        
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var result = await service.ValidateDelete(creditCard.Id);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDelete_ShouldReturnFailure_WhenCreditCardNotFound()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];

        var result = await service.ValidateDelete(nonExistentId);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardDeleteErrorCode.CreditCardNotFound);
        result.Message.Should().Be("CreditCard not found to delete.");
    }

    // NOTE: 'TODO here validate relations' exists in the service, but since the implementation is missing, we only test the existing logic.

    #endregion

    #region ValidateInput (Create and Update)

    private CreditCardInput GetValidInput() => new()
    {
        Name = "New Card",
        Color = "#FFFFFF",
        Icon = "fa-credit-card",
        Limit = 1000m,
        DueDay = 15,
        ClosingDay = 5,
        CardBrandId = TestUtils.Guids[0],
        DebitWalletId = TestUtils.Guids[1],
        FinancialInstitutionId = TestUtils.Guids[2]
    };

    private void SetupDependenciesSuccess(Resources resources)
    {
        var fiId = TestUtils.Guids[2];
        var walletId = TestUtils.Guids[1];
        var brandId = TestUtils.Guids[0];
        
        var activeInstitution = new FinancialInstitutionOutput { Id = fiId, Inactive = false };
        resources.FakeFinancialInstitution.Setup(s => s.Get(fiId)).ReturnsAsync(activeInstitution);
        
        var activeWallet = new WalletOutput { Id = walletId, Inactivated = false };
        resources.FakeWalletService.Setup(s => s.Get(walletId)).ReturnsAsync(activeWallet);
        
        resources.FakeCardBrandService.Setup(s => s.Get(brandId)).ReturnsAsync(new CardBrandOutput { Id = brandId });
    }

    [Fact]
    public async Task ValidateInput_Create_ShouldReturnSuccess_WhenValid()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        SetupDependenciesSuccess(resources);

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateInput_Update_ShouldReturnSuccess_WhenValid()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var originalInput = GetValidInput();
        var creditCard = new CreditCard(originalInput);
        creditCard.CardBrand = TestUtils.CardBrands[0];
        creditCard.FinancialInstitution = TestUtils.FinancialInstitutions[0];
        creditCard.DebitWallet = TestUtils.Wallets[0];
        creditCard.DebitWallet.FinancialInstitution = creditCard.FinancialInstitution;
        
        await resources.CreditCardRepository.AddAsync(creditCard, true);
        var input = GetValidInput();
        SetupDependenciesSuccess(resources);

        var result = await service.ValidateInput<bool>(input, creditCard.Id);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateInput_Update_ShouldReturnFailure_WhenCreditCardNotFound()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];
        var input = GetValidInput();

        var result = await service.ValidateInput<bool>(input, nonExistentId);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.CreditCardNotFound);
        result.Message.Should().Be("CreditCard not found to edit.");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameIsRequired(string name)
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Name = name;

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.NameIsRequired);
        result.Message.Should().Be("Name is required.");
    }
    
    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameTooLong()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Name = new string('A', 101);

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.NameTooLong);
        result.Message.Should().Be("Name is too long. Max 100 characters.");
    }
    
    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameAlreadyInUseOnCreate()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var existingName = TestUtils.Strings[0];

        var creditCard = new CreditCard(new CreditCardInput
        {
            Name = existingName, CardBrandId = TestUtils.Guids[9], DebitWalletId = TestUtils.Guids[9],
            Color = TestUtils.Strings[9],
            Icon = TestUtils.Strings[9],
        });
        creditCard.CardBrand = TestUtils.CardBrands[0];
        creditCard.FinancialInstitution = TestUtils.FinancialInstitutions[0];
        creditCard.DebitWallet = TestUtils.Wallets[0];
        creditCard.DebitWallet.FinancialInstitution = creditCard.FinancialInstitution;
        
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var input = GetValidInput();
        input.Name = existingName;

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.NameAlreadyInUse);
        result.Message.Should().Be("Name is already in use.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenFinancialInstitutionNotFound()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        resources.FakeFinancialInstitution.Setup(s => s.Get(input.FinancialInstitutionId)).ReturnsAsync((FinancialInstitutionOutput)null);
        resources.FakeWalletService.Setup(s => s.Get(input.DebitWalletId)).ReturnsAsync(new WalletOutput());
        resources.FakeCardBrandService.Setup(s => s.Get(input.CardBrandId)).ReturnsAsync(new CardBrandOutput());

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.FinancialInstitutionNotFound);
        result.Message.Should().Be("Financial institution not found.");
    }
    
    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenFinancialInstitutionInactivated()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        var inactiveInstitution = new FinancialInstitutionOutput { Id = input.FinancialInstitutionId, Inactive = true };
        resources.FakeFinancialInstitution.Setup(s => s.Get(input.FinancialInstitutionId)).ReturnsAsync(inactiveInstitution);
        resources.FakeWalletService.Setup(s => s.Get(input.DebitWalletId)).ReturnsAsync(new WalletOutput());
        resources.FakeCardBrandService.Setup(s => s.Get(input.CardBrandId)).ReturnsAsync(new CardBrandOutput());

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.FinancialInstitutionInactivated);
        result.Message.Should().Be("Financial institution is inactive.");
    }
    
    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenDebitWalletNotFound()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        var activeInstitution = new FinancialInstitutionOutput { Id = input.FinancialInstitutionId, Inactive = false };
        resources.FakeFinancialInstitution.Setup(s => s.Get(input.FinancialInstitutionId)).ReturnsAsync(activeInstitution);
        resources.FakeWalletService.Setup(s => s.Get(input.DebitWalletId)).ReturnsAsync((WalletOutput)null);
        resources.FakeCardBrandService.Setup(s => s.Get(input.CardBrandId)).ReturnsAsync(new CardBrandOutput());

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.DebitWalletNotFound);
        result.Message.Should().Be("Debit wallet not found.");
    }
    
    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenDebitWalletInactivated()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        var activeInstitution = new FinancialInstitutionOutput { Id = input.FinancialInstitutionId, Inactive = false };
        resources.FakeFinancialInstitution.Setup(s => s.Get(input.FinancialInstitutionId)).ReturnsAsync(activeInstitution);
        
        var inactiveWallet = new WalletOutput { Id = input.DebitWalletId, Inactivated = true };
        resources.FakeWalletService.Setup(s => s.Get(input.DebitWalletId)).ReturnsAsync(inactiveWallet);
        resources.FakeCardBrandService.Setup(s => s.Get(input.CardBrandId)).ReturnsAsync(new CardBrandOutput());

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.DebitWalletInactivated);
        result.Message.Should().Be("Debit wallet is inactive.");
    }
    
    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenCardBrandNotFound()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        var activeInstitution = new FinancialInstitutionOutput { Id = input.FinancialInstitutionId, Inactive = false };
        resources.FakeFinancialInstitution.Setup(s => s.Get(input.FinancialInstitutionId)).ReturnsAsync(activeInstitution);
        
        var activeWallet = new WalletOutput { Id = input.DebitWalletId, Inactivated = false };
        resources.FakeWalletService.Setup(s => s.Get(input.DebitWalletId)).ReturnsAsync(activeWallet);
        
        resources.FakeCardBrandService.Setup(s => s.Get(input.CardBrandId)).ReturnsAsync((CardBrandOutput)null);

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.CardBrandNotFound);
        result.Message.Should().Be("CardBrand not found.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateInput_ShouldReturnFailure_WhenColorIsRequired(string color)
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Color = color;
        SetupDependenciesSuccess(resources);

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.ColorIsRequired);
        result.Message.Should().Be("Color is required.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenColorTooLong()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Color = new string('A', 21);

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.ColorTooLong);
        result.Message.Should().Be("Color is too long. Max 20 characters.");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateInput_ShouldReturnFailure_WhenIconIsRequired(string icon)
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Icon = icon;
        SetupDependenciesSuccess(resources);

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.IconIsRequired);
        result.Message.Should().Be("Icon is required.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenIconTooLong()
    {
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Icon = new string('A', 21);

        var result = await service.ValidateInput<bool>(input);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.IconTooLong);
        result.Message.Should().Be("Icon is too long. Max 20 characters.");
    }

    #endregion
}