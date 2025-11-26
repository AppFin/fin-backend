using Fin.Application.CreditCards.Dtos;
using Fin.Application.CreditCards.Enums;
using Fin.Application.CreditCards.Services;
using Fin.Application.Globals.Dtos;
using Fin.Domain.CardBrands.Entities;
using Fin.Domain.CreditCards.Dtos;
using Fin.Domain.CreditCards.Entities;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fin.Test.CreditCards.Services;

public class CreditCardServiceTest : TestUtils.BaseTestWithContext
{
    private readonly Mock<ICreditCardValidationService> _validationServiceMock;

    public CreditCardServiceTest()
    {
        _validationServiceMock = new Mock<ICreditCardValidationService>();
    }

    private CreditCardService GetService(Resources resources)
    {
        return new CreditCardService(resources.CreditCardRepository, _validationServiceMock.Object);
    }

    private async Task<Resources> GetResources()
    {
        var resource = new Resources
        {
            CreditCardRepository = GetRepository<CreditCard>(),
            DebitWalletId = TestUtils.Guids[9],
            CardBrandId = TestUtils.Guids[8],
            FinancialInstitutionId = TestUtils.Guids[7]
        };
        
        var financialInstitution = TestUtils.FinancialInstitutions[0];
        financialInstitution.Id = resource.FinancialInstitutionId;
        var cardBrand = TestUtils.CardBrands[0];
        cardBrand.Id = resource.CardBrandId;
        var wallet = TestUtils.Wallets[0];
        wallet.Id = resource.DebitWalletId;
        wallet.FinancialInstitution = financialInstitution;
        
        await GetRepository<Wallet>().AddAsync(wallet, true);
        await GetRepository<CardBrand>().AddAsync(cardBrand, true);
        
        return resource;
    }

    private CreditCard CreateCreditCard(CreditCardInput creditCard, Resources resources)
    {
        creditCard.FinancialInstitutionId = resources.FinancialInstitutionId;
        creditCard.DebitWalletId = resources.DebitWalletId;
        creditCard.CardBrandId = resources.CardBrandId;
        creditCard.Color = TestUtils.Strings[8];
        creditCard.Icon = TestUtils.Strings[9];
        return new CreditCard(creditCard);
    }

    private class Resources
    {
        public IRepository<CreditCard> CreditCardRepository { get; set; }
        public Guid FinancialInstitutionId { get; set; }
        public Guid DebitWalletId { get; set; }
        public Guid CardBrandId { get; set; }
    }

    #region Get

    [Fact]
    public async Task Get_ShouldReturnCreditCard_WhenExists()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var input = new CreditCardInput
        {
            Name = "Visa Test", Limit = 5000, DueDay = 15, ClosingDay = 5, Color = TestUtils.Strings[9], Icon = TestUtils.Strings[9]
        };
        var creditCard = CreateCreditCard(input, resources);
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var result = await service.Get(creditCard.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(creditCard.Id);
        result.Name.Should().Be(input.Name);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenNotExists()
    {
        var resources = await GetResources();
        var service = GetService(resources);

        var result = await service.Get(TestUtils.Guids[9]);

        result.Should().BeNull();
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult_WithoutFilter()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var brandId = TestUtils.Guids[0];
        var walletId = TestUtils.Guids[1];
        var fiId = TestUtils.Guids[2];

        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "C", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId, DebitWalletId = walletId,
                FinancialInstitutionId = fiId
            }, resources), true);
        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "A", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId, DebitWalletId = walletId,
                FinancialInstitutionId = fiId
            }, resources), true);
        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "B", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId, DebitWalletId = walletId,
                FinancialInstitutionId = fiId
            }, resources), true);

        var input = new CreditCardGetListInput { MaxResultCount = 2, SkipCount = 0 };

        var result = await service.GetList(input);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("A");
        result.Items.Last().Name.Should().Be("B");
    }

    [Fact]
    public async Task GetList_ShouldFilterByInactivatedTrue()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var brandId = resources.CardBrandId;
        var walletId = resources.DebitWalletId;
        var fiId = resources.FinancialInstitutionId;

        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "Active1", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId,
                DebitWalletId = walletId, FinancialInstitutionId = fiId, Color = TestUtils.Strings[9], Icon = TestUtils.Strings[9]
            }, resources), true);

        var inactive = CreateCreditCard(new CreditCardInput
        {
            Name = "Inactive1", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId,
            DebitWalletId = walletId, FinancialInstitutionId = fiId, Color = TestUtils.Strings[9], Icon = TestUtils.Strings[9]
        }, resources);
        inactive.ToggleInactivated();
        await resources.CreditCardRepository.AddAsync(inactive, true);

        var inactive2 = CreateCreditCard(new CreditCardInput
        {
            Name = "Inactive2", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId,
            DebitWalletId = walletId, FinancialInstitutionId = fiId, Color = TestUtils.Strings[9], Icon = TestUtils.Strings[9]
        }, resources);
        inactive2.ToggleInactivated();
        await resources.CreditCardRepository.AddAsync(inactive2, true);

        var input = new CreditCardGetListInput { Inactivated = true, MaxResultCount = 10, SkipCount = 0 };
            
        var result = await service.GetList(input);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("Inactive1");
        result.Items.Last().Name.Should().Be("Inactive2");
    }

    [Fact]
    public async Task GetList_ShouldFilterByMultipleIds()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var brandId0 = TestUtils.Guids[0];
        var brandId1 = TestUtils.Guids[1];
        var walletId2 = TestUtils.Guids[2];
        var walletId3 = TestUtils.Guids[3];
        var fiId4 = TestUtils.Guids[4];
        var fiId5 = TestUtils.Guids[5];

        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "CardA", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId0,
                DebitWalletId = walletId2, FinancialInstitutionId = fiId4
            }, resources), true);
        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "CardB", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId1,
                DebitWalletId = walletId3, FinancialInstitutionId = fiId4
            }, resources), true);
        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "CardC", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId0,
                DebitWalletId = walletId3, FinancialInstitutionId = fiId5
            }, resources), true);

        var input = new CreditCardGetListInput
        {
            CardBrandIds = [brandId0],
            DebitWalletIds = [walletId3],
            FinancialInstitutionIds = [fiId4],
            MaxResultCount = 10,
            SkipCount = 0
        };

        var result = await service.GetList(input);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetList_ShouldFilterCorrectly_WhenOneFilterIsMatch()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var brandId0 = resources.CardBrandId;
        var brandId1 = TestUtils.Guids[5];
        var walletId2 = resources.DebitWalletId;
        var fiId4 = resources.FinancialInstitutionId;

        var cardBrand1 = TestUtils.CardBrands[1];
        cardBrand1.Id = brandId1;
        await GetRepository<CardBrand>().AddAsync(cardBrand1);

        await resources.CreditCardRepository.AddAsync(
            CreateCreditCard(new CreditCardInput
            {
                Name = "CardA", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId0,
                DebitWalletId = walletId2, FinancialInstitutionId = fiId4
            }, resources), true);
        await resources.CreditCardRepository.AddAsync(
            new CreditCard(new CreditCardInput
            {
                Name = "CardB", Limit = 100, DueDay = 1, ClosingDay = 1, CardBrandId = brandId1,
                DebitWalletId = walletId2, FinancialInstitutionId = fiId4, Icon = TestUtils.Strings[1], Color = TestUtils.Strings[1],
            }), true);

        var input = new CreditCardGetListInput
        {
            CardBrandIds = [brandId1],
            MaxResultCount = 10,
            SkipCount = 0
        };

        var result = await service.GetList(input);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("CardB");
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnSuccessAndCreditCard_WhenInputIsValid()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var input = new CreditCardInput
        {
            Name = "Mastercard Black",
            Color = "#000000",
            Icon = "cc-mastercard",
            Limit = 15000.50m,
            DueDay = 15,
            ClosingDay = 5,
            CardBrandId = resources.CardBrandId,
            DebitWalletId = resources.DebitWalletId,
            FinancialInstitutionId = resources.FinancialInstitutionId
        };

        var successValidation = new ValidationResultDto<CreditCardOutput, CreditCardCreateOrUpdateErrorCode>
            { Success = true };
        _validationServiceMock.Setup(v => v.ValidateInput<CreditCardOutput>(input, null))
            .ReturnsAsync(successValidation);

        var result = await service.Create(input, true);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var dbCreditCard = await resources.CreditCardRepository.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == result.Data.Id);
        dbCreditCard.Should().NotBeNull();
        dbCreditCard.Name.Should().Be(input.Name);
        dbCreditCard.Limit.Should().Be(input.Limit);
        dbCreditCard.ClosingDay.Should().Be(input.ClosingDay);
        dbCreditCard.DebitWalletId.Should().Be(input.DebitWalletId);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenValidationFails()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var input = new CreditCardInput { Name = null };

        var failureValidation = new ValidationResultDto<CreditCardOutput, CreditCardCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardCreateOrUpdateErrorCode.NameIsRequired,
            Message = "Name is required."
        };
        _validationServiceMock.Setup(v => v.ValidateInput<CreditCardOutput>(input, null))
            .ReturnsAsync(failureValidation);

        var result = await service.Create(input, true);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.NameIsRequired);
        result.Data.Should().BeNull();

        (await resources.CreditCardRepository.AsNoTracking().CountAsync()).Should().Be(0);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnSuccess_WhenInputIsValid()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var originalInput = new CreditCardInput
        {
            Name = "Old Card", Limit = 1000, DueDay = 1, ClosingDay = 25, CardBrandId = TestUtils.Guids[0],
            DebitWalletId = TestUtils.Guids[1], FinancialInstitutionId = TestUtils.Guids[2]
        };
        var creditCard = CreateCreditCard(originalInput, resources);
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var updatedInput = new CreditCardInput
        {
            Name = "New Card Name",
            Color = "#FFFFFF",
            Icon = "cc-visa",
            Limit = 5000.75m,
            DueDay = 10,
            ClosingDay = 30,
            CardBrandId = resources.CardBrandId,
            DebitWalletId = resources.DebitWalletId,
            FinancialInstitutionId = resources.FinancialInstitutionId
        };

        var successValidation = new ValidationResultDto<bool, CreditCardCreateOrUpdateErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateInput<bool>(updatedInput, creditCard.Id))
            .ReturnsAsync(successValidation);

        var result = await service.Update(creditCard.Id, updatedInput, true);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var dbCreditCard = await resources.CreditCardRepository.AsNoTracking().FirstAsync(a => a.Id == creditCard.Id);
        dbCreditCard.Name.Should().Be(updatedInput.Name);
        dbCreditCard.Limit.Should().Be(updatedInput.Limit);
        dbCreditCard.DueDay.Should().Be(updatedInput.DueDay);
        dbCreditCard.ClosingDay.Should().Be(updatedInput.ClosingDay);
        dbCreditCard.CardBrandId.Should().Be(updatedInput.CardBrandId);
        dbCreditCard.DebitWalletId.Should().Be(updatedInput.DebitWalletId);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenValidationFails()
    {
        var resources = await  GetResources();
        var service = GetService(resources);
        var originalInput = new CreditCardInput
        {
            Name = "Old Card", Limit = 1000, DueDay = 1, ClosingDay = 25, CardBrandId = TestUtils.Guids[0],
            DebitWalletId = TestUtils.Guids[1], FinancialInstitutionId = TestUtils.Guids[2]
        };
        var creditCard = CreateCreditCard(originalInput, resources);
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var updatedInput = new CreditCardInput { Name = TestUtils.Strings[4], Limit = 5000 };

        var failureValidation = new ValidationResultDto<bool, CreditCardCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardCreateOrUpdateErrorCode.CardBrandNotFound,
            Message = "Card Brand not found."
        };
        _validationServiceMock.Setup(v => v.ValidateInput<bool>(updatedInput, creditCard.Id))
            .ReturnsAsync(failureValidation);

        var result = await service.Update(creditCard.Id, updatedInput, true);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardCreateOrUpdateErrorCode.CardBrandNotFound);
        result.Data.Should().BeFalse();

        var dbCreditCard = await resources.CreditCardRepository.AsNoTracking().FirstAsync(a => a.Id == creditCard.Id);
        dbCreditCard.Name.Should().Be(originalInput.Name);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValid()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var creditCard = CreateCreditCard(new CreditCardInput
        {
            Name = TestUtils.Strings[0], Limit = 1000, DueDay = 1, ClosingDay = 25, Color = TestUtils.Strings[9], Icon = TestUtils.Strings[9]
        }, resources);
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var successValidation = new ValidationResultDto<bool, CreditCardDeleteErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateDelete(creditCard.Id)).ReturnsAsync(successValidation);

        var result = await service.Delete(creditCard.Id, true);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        (await resources.CreditCardRepository.AsNoTracking().FirstOrDefaultAsync(a => a.Id == creditCard.Id)).Should()
            .BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenValidationFails()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var creditCard = CreateCreditCard(new CreditCardInput
        {
            Name = TestUtils.Strings[0], Limit = 1000, DueDay = 1, ClosingDay = 25, Color = TestUtils.Strings[9], Icon = TestUtils.Strings[9]
        }, resources);
        await resources.CreditCardRepository.AddAsync(creditCard, true);

        var failureValidation = new ValidationResultDto<bool, CreditCardDeleteErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardDeleteErrorCode.CreditCardInUse,
            Message = "Cannot delete credit card with transactions."
        };
        _validationServiceMock.Setup(v => v.ValidateDelete(creditCard.Id)).ReturnsAsync(failureValidation);

        var result = await service.Delete(creditCard.Id, true);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardDeleteErrorCode.CreditCardInUse);
        (await resources.CreditCardRepository.AsNoTracking().FirstOrDefaultAsync(a => a.Id == creditCard.Id)).Should()
            .NotBeNull();
    }

    #endregion

    #region ToggleInactive

    [Fact]
    public async Task ToggleInactive_ShouldReturnSuccess_WhenValidAndDeactivate()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var creditCard = CreateCreditCard(new CreditCardInput
        {
            Name = TestUtils.Strings[0], Limit = 1000, DueDay = 1, ClosingDay = 25, CardBrandId = TestUtils.Guids[0],
            DebitWalletId = TestUtils.Guids[1], FinancialInstitutionId = TestUtils.Guids[2]
        }, resources);
        await resources.CreditCardRepository.AddAsync(creditCard, true);
        creditCard.Inactivated.Should().BeFalse();

        var successValidation = new ValidationResultDto<bool, CreditCardToggleInactiveErrorCode> { Success = true };
        _validationServiceMock.Setup(v => v.ValidateToggleInactive(creditCard.Id)).ReturnsAsync(successValidation);

        var result = await service.ToggleInactive(creditCard.Id, true);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        var dbCreditCard = await resources.CreditCardRepository.AsNoTracking().FirstAsync(a => a.Id == creditCard.Id);
        dbCreditCard.Inactivated.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnFailure_WhenValidationFails()
    {
        var resources = await GetResources();
        var service = GetService(resources);
        var creditCard = CreateCreditCard(new CreditCardInput
        {
            Name = TestUtils.Strings[0], Limit = 1000, DueDay = 1, ClosingDay = 25, CardBrandId = TestUtils.Guids[0],
            DebitWalletId = TestUtils.Guids[1], FinancialInstitutionId = TestUtils.Guids[2]
        }, resources);
        await resources.CreditCardRepository.AddAsync(creditCard, true);
        creditCard.Inactivated.Should().BeFalse();

        var failureValidation = new ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>
        {
            Success = false,
            ErrorCode = CreditCardToggleInactiveErrorCode.CreditCardNotFound,
            Message = "Credit Card not found."
        };
        _validationServiceMock.Setup(v => v.ValidateToggleInactive(creditCard.Id)).ReturnsAsync(failureValidation);

        var result = await service.ToggleInactive(creditCard.Id, true);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(CreditCardToggleInactiveErrorCode.CreditCardNotFound);
        var dbCreditCard = await resources.CreditCardRepository.AsNoTracking().FirstAsync(a => a.Id == creditCard.Id);
        dbCreditCard.Inactivated.Should().BeFalse();
    }

    #endregion
}