using Fin.Application.Globals.Dtos;
using Fin.Application.Wallets.Dtos;
using Fin.Application.Wallets.Enums;
using Fin.Domain.Global.Classes;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Wallets.Services;

public interface IWalletService
{
    public Task<WalletOutput> Get(Guid id);
    public Task<PagedOutput<WalletOutput>> GetList(WalletGetListInput input);
    public Task<ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>> Create(WalletInput input, bool autoSave = false);
    public Task<ValidationResultDto<bool, WalletCreateOrUpdateErrorCode>> Update(Guid id, WalletInput input, bool autoSave = false);
    public Task<ValidationResultDto<bool, WalletDeleteErrorCode>> Delete(Guid id, bool autoSave = false);
    public Task<ValidationResultDto<bool, WalletToogleInactiveErrorCode>> ToggleInactive(Guid id, bool autoSave = false);
}

public class WalletService(
    IRepository<Wallet> repository,
    IWalletValidationService validationService
    ) : IWalletService, IAutoTransient
{
    public async Task<WalletOutput> Get(Guid id)
    {
        var entity = await repository.Query(false).FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new WalletOutput(entity) : null;
    }

    public async Task<PagedOutput<WalletOutput>> GetList(WalletGetListInput input)
    {
        return await repository.Query(false)
            .WhereIf(input.Inactivated.HasValue, n => n.Inactivated == input.Inactivated.Value)
            .OrderBy(m => m.Inactivated)
            .ThenBy(m => m.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new WalletOutput(n))
            .ToPagedResult(input);
    }

    public async Task<ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>> Create(WalletInput input, bool autoSave = false)
    {
        var validation = await validationService.ValidateInput<WalletOutput>(input);
        if (!validation.Success) return validation;
        
        var wallet = new Wallet(input);
        await repository.AddAsync(wallet, autoSave);
        validation.Data = new WalletOutput(wallet);
        return validation;
    }

    public async Task<ValidationResultDto<bool, WalletCreateOrUpdateErrorCode>> Update(Guid id, WalletInput input, bool autoSave = false)
    {
        var validation = await validationService.ValidateInput<bool>(input, id);
        if (!validation.Success) return validation;
        
        var wallet = await repository.Query().FirstAsync(u => u.Id == id);
        wallet.Update(input);
        await repository.UpdateAsync(wallet, autoSave);

        validation.Data = true;
        return validation;   
    }

    public async Task<ValidationResultDto<bool, WalletDeleteErrorCode>> Delete(Guid id, bool autoSave = false)
    {
        var validation = await validationService.ValidateDelete(id);
        if (!validation.Success) return validation;
        
        var wallet = await repository.Query().FirstAsync(u => u.Id == id);
        await repository.DeleteAsync(wallet, autoSave);

        validation.Success = true;
        return validation;
    }

    public async Task<ValidationResultDto<bool, WalletToogleInactiveErrorCode>> ToggleInactive(Guid id, bool autoSave = false)
    {
        var validation = await validationService.ValidateToggleInactive(id);
        if (!validation.Success) return validation;
        
        var wallet = await repository.Query().FirstAsync(u => u.Id == id);
        wallet.ToggleInactivated();
        await repository.UpdateAsync(wallet, autoSave);

        validation.Data = true;
        return validation; 
    }
}