using Fin.Application.CardBrands;
using Fin.Application.FinancialInstitutions;
using Fin.Application.Globals.Dtos;
using Fin.Application.CreditCards.Enums;
using Fin.Application.Wallets.Services;
using Fin.Domain.CreditCards.Dtos;
using Fin.Domain.CreditCards.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CreditCards.Services;

public interface ICreditCardValidationService
{
    public Task<ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>> ValidateToggleInactive(Guid creditCardId);
    public Task<ValidationResultDto<bool, CreditCardDeleteErrorCode>> ValidateDelete(Guid creditCardId);

    public Task<ValidationResultDto<T, CreditCardCreateOrUpdateErrorCode>> ValidateInput<T>(CreditCardInput input,
        Guid? editingId = null);
}

public class CreditCardValidationService(
    IRepository<CreditCard> repository,
    IFinancialInstitutionService financialInstitutionService,
    IWalletService walletService,
    ICardBrandService cardBrandService
) : ICreditCardValidationService, IAutoTransient
{
    public async Task<ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>> ValidateToggleInactive(Guid creditCardId)
    {
        var validationResult = new ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>();

        var creditCard = await repository.Query(tracking: false).FirstOrDefaultAsync(n => n.Id == creditCardId);
        if (creditCard is null)
        {
            validationResult.ErrorCode = CreditCardToggleInactiveErrorCode.CreditCardNotFound;
            validationResult.Message = "CreditCard not found to toggle inactive.";
            return validationResult;
        }

        validationResult.Success = true;
        return validationResult;
    }

    public async Task<ValidationResultDto<bool, CreditCardDeleteErrorCode>> ValidateDelete(Guid creditCardId)
    {
        var validationResult = new ValidationResultDto<bool, CreditCardDeleteErrorCode>();

        var creditCardExists = await repository.Query().AnyAsync(n => n.Id == creditCardId);
        if (!creditCardExists)
        {
            validationResult.ErrorCode = CreditCardDeleteErrorCode.CreditCardNotFound;
            validationResult.Message = "CreditCard not found to delete.";
            return validationResult;
        }

        // TODO here validate relations

        validationResult.Success = true;
        return validationResult;
    }

    public async Task<ValidationResultDto<T, CreditCardCreateOrUpdateErrorCode>> ValidateInput<T>(CreditCardInput input,
        Guid? editingId = null)
    {
        var validationResult = new ValidationResultDto<T, CreditCardCreateOrUpdateErrorCode>();

        if (editingId.HasValue)
        {
            var creditCardExists = await repository.Query()
                .AnyAsync(n => n.Id == editingId.Value);
            if (!creditCardExists)
            {
                validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.CreditCardNotFound;
                validationResult.Message = "CreditCard not found to edit.";
                return validationResult;
            }
        }

        if (string.IsNullOrWhiteSpace(input.Color))
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.ColorIsRequired;
            validationResult.Message = "Color is required.";
            return validationResult;
        }

        if (input.Color.Length > 20)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.ColorTooLong;
            validationResult.Message = "Color is too long. Max 20 characters.";
            return validationResult;
        }

        if (string.IsNullOrWhiteSpace(input.Icon))
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.IconIsRequired;
            validationResult.Message = "Icon is required.";
            return validationResult;
        }

        if (input.Icon.Length > 20)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.IconTooLong;
            validationResult.Message = "Icon is too long. Max 20 characters.";
            return validationResult;
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.NameIsRequired;
            validationResult.Message = "Name is required.";
            return validationResult;
        }

        if (input.Name.Length > 100)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.NameTooLong;
            validationResult.Message = "Name is too long. Max 100 characters.";
            return validationResult;
        }

        var nameAlredInUse = await repository.Query()
            .AnyAsync(n => n.Name.ToLower() == input.Name.ToLower()  && (!editingId.HasValue || n.Id != editingId));
        if (nameAlredInUse)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.NameAlreadyInUse;
            validationResult.Message = "Name is already in use.";
            return validationResult;
        }

        var financialInstitution = await financialInstitutionService.Get(input.FinancialInstitutionId);
        if (financialInstitution is null)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.FinancialInstitutionNotFound;
            validationResult.Message = "Financial institution not found.";
            return validationResult;
        }
        if (financialInstitution.Inactive)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.FinancialInstitutionInactivated;
            validationResult.Message = "Financial institution is inactive.";
            return validationResult;
        }
        
        var wallet = await walletService.Get(input.DebitWalletId);
        if (wallet is null)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.DebitWalletNotFound;
            validationResult.Message = "Debit wallet not found.";
            return validationResult;
        }
        if (wallet.Inactivated)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.DebitWalletInactivated;
            validationResult.Message = "Debit wallet is inactive.";
            return validationResult;
        }
        
        var cardBrand = await cardBrandService.Get(input.CardBrandId);
        if (cardBrand is null)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.CardBrandNotFound;
            validationResult.Message = "CardBrand not found.";
            return validationResult;
        }

        if (input.Limit <= 0)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.LimitMinValueZero;
            validationResult.Message = "Limit must be greater than zero.";
            return validationResult;
        }
        if (1 > input.DueDay || input.DueDay > 31)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.DueDayOutOfRange;
            validationResult.Message = "Due day is out of range. >= 1 and <= 31";
            return validationResult;
        }
        if (1 > input.ClosingDay || input.ClosingDay > 31)
        {
            validationResult.ErrorCode = CreditCardCreateOrUpdateErrorCode.ClosingDayOutOfRange;
            validationResult.Message = "Closing day is out of range. >= 1 and <= 31";
            return validationResult;
        }

        validationResult.Success = true;
        return validationResult;
    }
}