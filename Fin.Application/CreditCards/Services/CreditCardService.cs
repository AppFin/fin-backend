using Fin.Application.Globals.Dtos;
using Fin.Application.CreditCards.Dtos;
using Fin.Application.CreditCards.Enums;
using Fin.Domain.Global.Classes;
using Fin.Domain.CreditCards.Dtos;
using Fin.Domain.CreditCards.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CreditCards.Services;

public interface ICreditCardService
{
    public Task<CreditCardOutput> Get(Guid id);
    public Task<PagedOutput<CreditCardOutput>> GetList(CreditCardGetListInput input);
    public Task<ValidationResultDto<CreditCardOutput, CreditCardCreateOrUpdateErrorCode>> Create(CreditCardInput input, bool autoSave = false);
    public Task<ValidationResultDto<bool, CreditCardCreateOrUpdateErrorCode>> Update(Guid id, CreditCardInput input, bool autoSave = false);
    public Task<ValidationResultDto<bool, CreditCardDeleteErrorCode>> Delete(Guid id, bool autoSave = false);
    public Task<ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>> ToggleInactive(Guid id, bool autoSave = false);
}

public class CreditCardService(
    IRepository<CreditCard> repository,
    ICreditCardValidationService validationService
    ) : ICreditCardService, IAutoTransient
{
    public async Task<CreditCardOutput> Get(Guid id)
    {
        var entity = await repository.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new CreditCardOutput(entity) : null;
    }

    public async Task<PagedOutput<CreditCardOutput>> GetList(CreditCardGetListInput input)
    {
        return await repository.AsNoTracking()
            .WhereIf(input.Inactivated.HasValue, n => n.Inactivated == input.Inactivated.Value)
            .WhereIf(input.CardBrandIds.Any(), creditCard => input.CardBrandIds.Contains(creditCard.CardBrandId))
            .WhereIf(input.DebitWalletIds.Any(), creditCard => input.DebitWalletIds.Contains(creditCard.DebitWalletId))
            .WhereIf(input.FinancialInstitutionIds.Any(), creditCard => input.FinancialInstitutionIds.Contains(creditCard.FinancialInstitutionId))
            .OrderBy(m => m.Inactivated)
            .ThenBy(m => m.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new CreditCardOutput(n))
            .ToPagedResult(input);
    }

    public async Task<ValidationResultDto<CreditCardOutput, CreditCardCreateOrUpdateErrorCode>> Create(CreditCardInput input, bool autoSave = false)
    {
        var validation = await validationService.ValidateInput<CreditCardOutput>(input);
        if (!validation.Success) return validation;
        
        var creditCard = new CreditCard(input);
        await repository.AddAsync(creditCard, autoSave);
        validation.Data = new CreditCardOutput(creditCard);
        return validation;
    }

    public async Task<ValidationResultDto<bool, CreditCardCreateOrUpdateErrorCode>> Update(Guid id, CreditCardInput input, bool autoSave = false)
    {
        var validation = await validationService.ValidateInput<bool>(input, id);
        if (!validation.Success) return validation;
        
        var creditCard = await repository.FirstAsync(u => u.Id == id);
        creditCard.Update(input);
        await repository.UpdateAsync(creditCard, autoSave);

        validation.Data = true;
        return validation;   
    }

    public async Task<ValidationResultDto<bool, CreditCardDeleteErrorCode>> Delete(Guid id, bool autoSave = false)
    {
        var validation = await validationService.ValidateDelete(id);
        if (!validation.Success) return validation;
        
        var creditCard = await repository.FirstAsync(u => u.Id == id);
        await repository.DeleteAsync(creditCard, autoSave);

        validation.Success = true;
        return validation;
    }

    public async Task<ValidationResultDto<bool, CreditCardToggleInactiveErrorCode>> ToggleInactive(Guid id, bool autoSave = false)
    {
        var validation = await validationService.ValidateToggleInactive(id);
        if (!validation.Success) return validation;
        
        var creditCard = await repository.FirstAsync(u => u.Id == id);
        creditCard.ToggleInactivated();
        await repository.UpdateAsync(creditCard, autoSave);

        validation.Data = true;
        return validation; 
    }
}