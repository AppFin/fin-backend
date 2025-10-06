using Fin.Application.Globals.Dtos;
using Fin.Application.Wallets.Enums;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Wallets.Services;

public interface IWalletValidationService
{
    public Task<ValidationResultDto<bool, WalletToogleInactiveErrorCode>> ValidateToggleInactive(Guid walletId);
    public Task<ValidationResultDto<bool, WalletDeleteErrorCode>> ValidateDelete(Guid walletId);
    public Task<ValidationResultDto<T,WalletCreateOrUpdateErrorCode>> ValidateInput<T>(WalletInput input, Guid? editingId = null);
}

public class WalletValidationService(IRepository<Wallet>repository): IWalletValidationService, IAutoTransient
{
    public async Task<ValidationResultDto<bool, WalletToogleInactiveErrorCode>> ValidateToggleInactive(Guid walletId)
    {
        var validationResult = new ValidationResultDto<bool, WalletToogleInactiveErrorCode>();
        
        var wallet = await repository.Query(tracking: false).FirstOrDefaultAsync(n => n.Id == walletId);
        if (wallet is null)
        {
            validationResult.ErrorCode = WalletToogleInactiveErrorCode.WalletNotFound;
            validationResult.Message = "Wallet not found to toogle inactive.";
            return validationResult;
        }
        
        // TODO here validate relations
        
        validationResult.Success = true;
        return validationResult;
    }
    
    public async Task<ValidationResultDto<bool, WalletDeleteErrorCode>> ValidateDelete(Guid walletId)
    {
        var validationResult = new ValidationResultDto<bool, WalletDeleteErrorCode>();
        
        var walletExists = await repository.Query().AnyAsync(n => n.Id == walletId);
        if (!walletExists)
        {
            validationResult.ErrorCode = WalletDeleteErrorCode.WalletNotFound;
            validationResult.Message = "Wallet not found to delete.";
            return validationResult;
        }
        
        // TODO here validate relations
        
        validationResult.Success = true;
        return validationResult;
    }

    public async Task<ValidationResultDto<T,WalletCreateOrUpdateErrorCode>> ValidateInput<T>(WalletInput input, Guid? editingId = null)
    {
        var validationResult = new ValidationResultDto<T,WalletCreateOrUpdateErrorCode>();

        if (editingId.HasValue)
        {
            var walletExists = await repository.Query()
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
        var nameAlredInUse = await repository.Query()
            .AnyAsync(n => n.Name == input.Name && (!editingId.HasValue || n.Id != editingId));
        if (nameAlredInUse)
        {
            validationResult.ErrorCode = WalletCreateOrUpdateErrorCode.NameAlreadyInUse;
            validationResult.Message = "Name is already in use.";
            return validationResult;
        }
        
        validationResult.Success = true;
        return validationResult;
    }
}