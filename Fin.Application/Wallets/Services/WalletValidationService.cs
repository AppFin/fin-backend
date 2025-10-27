using Fin.Application.FinancialInstitutions;
using Fin.Application.Globals.Dtos;
using Fin.Application.Wallets.Enums;
using Fin.Domain.CreditCards.Entities;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Wallets.Services;

public interface IWalletValidationService
{
    public Task<ValidationResultDto<bool, WalletToggleInactiveErrorCode>> ValidateToggleInactive(Guid walletId);
    public Task<ValidationResultDto<bool, WalletDeleteErrorCode>> ValidateDelete(Guid walletId);

    public Task<ValidationResultDto<T, WalletCreateOrUpdateErrorCode>> ValidateInput<T>(WalletInput input,
        Guid? editingId = null);
}

public class WalletValidationService(
    IRepository<Wallet> walletRepository,
    IRepository<CreditCard> creditCardRepository,
    IRepository<Title> titleRepository,
    IFinancialInstitutionService financialInstitutionService
) : IWalletValidationService, IAutoTransient
{
    public async Task<ValidationResultDto<bool, WalletToggleInactiveErrorCode>> ValidateToggleInactive(Guid walletId)
    {
        var validationResult = new ValidationResultDto<bool, WalletToggleInactiveErrorCode>();

        var wallet = await walletRepository.Query(tracking: false).FirstOrDefaultAsync(n => n.Id == walletId);
        if (wallet is null)
        {
            validationResult.ErrorCode = WalletToggleInactiveErrorCode.WalletNotFound;
            validationResult.Message = "Wallet not found to toggle inactive.";
            return validationResult;
        }

        var walletInUseByActivatedCreditCard = await creditCardRepository.Query().AnyAsync(n => n.DebitWalletId == walletId && !n.Inactivated);
        if (walletInUseByActivatedCreditCard)
        {
            validationResult.ErrorCode = WalletToggleInactiveErrorCode.WalletInUseByActivatedCreditCards;
            validationResult.Message = "Wallet in use by activated credit cards.";
            return validationResult;
        }

        validationResult.Success = true;
        return validationResult;
    }

    public async Task<ValidationResultDto<bool, WalletDeleteErrorCode>> ValidateDelete(Guid walletId)
    {
        var validationResult = new ValidationResultDto<bool, WalletDeleteErrorCode>();

        var walletExists = await walletRepository.Query().AnyAsync(n => n.Id == walletId);
        if (!walletExists)
        {
            validationResult.ErrorCode = WalletDeleteErrorCode.WalletNotFound;
            return validationResult;
        }

        var walletInUseByCreditCard = await creditCardRepository.Query().AnyAsync(n => n.DebitWalletId == walletId);
        var walletInUseByTitle = await titleRepository.Query().AnyAsync(n => n.WalletId == walletId);

        return walletInUseByTitle switch
        {
            true when walletInUseByCreditCard => validationResult.WithError(WalletDeleteErrorCode
                .WalletInUseByCreditCardsAndTitle),
            true => validationResult.WithError(WalletDeleteErrorCode.WalletInUseByTitles),
            false when walletInUseByCreditCard => validationResult.WithError(WalletDeleteErrorCode
                .WalletInUseByCreditCards),
            _ => validationResult
        };
    }

    public async Task<ValidationResultDto<T, WalletCreateOrUpdateErrorCode>> ValidateInput<T>(WalletInput input,
        Guid? editingId = null)
    {
        var validationResult = new ValidationResultDto<T, WalletCreateOrUpdateErrorCode>();

        if (editingId.HasValue)
        {
            var walletExists = await walletRepository.Query()
                .AnyAsync(n => n.Id == editingId.Value);
            if (!walletExists)
            {
                validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.WalletNotFound;
                validationResult.Message = "Wallet not found to edit.";
                return validationResult;
            }
        }

        if (string.IsNullOrWhiteSpace(input.Color))
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.ColorIsRequired;
            validationResult.Message = "Color is required.";
            return validationResult;
        }

        if (input.Color.Length > 20)
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.ColorTooLong;
            validationResult.Message = "Color is too long. Max 20 characters.";
            return validationResult;
        }

        if (string.IsNullOrWhiteSpace(input.Icon))
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.IconIsRequired;
            validationResult.Message = "Icon is required.";
            return validationResult;
        }

        if (input.Icon.Length > 20)
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.IconTooLong;
            validationResult.Message = "Icon is too long. Max 20 characters.";
            return validationResult;
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.NameIsRequired;
            validationResult.Message = "Name is required.";
            return validationResult;
        }

        if (input.Name.Length > 100)
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.NameTooLong;
            validationResult.Message = "Name is too long. Max 100 characters.";
            return validationResult;
        }

        var nameAlredInUse = await walletRepository.Query()
            .AnyAsync(n => n.Name.ToLower() == input.Name.ToLower()  && (!editingId.HasValue || n.Id != editingId));
        if (nameAlredInUse)
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.NameAlreadyInUse;
            validationResult.Message = "Name is already in use.";
            return validationResult;
        }

        if (input.FinancialInstitutionId.HasValue)
        {
            var financialInstitution = await financialInstitutionService.Get(input.FinancialInstitutionId.Value);
            if (financialInstitution is null)
            {
                validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.FinancialInstitutionNotFound;
                validationResult.Message = "Financial institution not found.";
                return validationResult;
            }

            if (financialInstitution.Inactive)
            {
                validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.FinancialInstitutionInactivated;
                validationResult.Message = "Financial institution is inactive.";
                return validationResult;
            }
        }

        validationResult.Success = true;
        return validationResult;
    }
}