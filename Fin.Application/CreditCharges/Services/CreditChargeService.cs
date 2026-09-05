using Fin.Application.CreditCharges.Dtos;
using Fin.Application.CreditCharges.Enums;
using Fin.Application.Globals.Dtos;
using Fin.Domain.CreditCards.Entities;
using Fin.Domain.CreditCharges.Entities;
using Fin.Domain.Global.Classes;
using Fin.Domain.Global.Enums;
using Fin.Domain.People.Entities;
using Fin.Domain.TitleCategories;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.UnitOfWorks;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CreditCharges.Services;

public interface ICreditChargeService
{
    public Task<CreditChargeOutput> Get(Guid id, CancellationToken cancellationToken = default);

    public Task<PagedOutput<CreditChargeOutput>> GetList(CreditChargeGetListInput input,
        CancellationToken cancellationToken = default);

    public Task<ValidationResultDto<CreditChargeOutput, CreditChargeCreateOrUpdateErrorCode>> Create(CreditChargeInput input,
        bool autoSave = false, CancellationToken cancellationToken = default);

    public Task<ValidationResultDto<bool, CreditChargeCreateOrUpdateErrorCode>> Update(Guid id, CreditChargeInput input,
        bool autoSave = false, CancellationToken cancellationToken = default);

    public Task<ValidationResultDto<bool, CreditChargeDeleteErrorCode>> Delete(Guid id, bool autoSave = false,
        CancellationToken cancellationToken = default);
}

public class CreditChargeService(
    IRepository<CreditCharge> chargeRepository,
    IRepository<CreditCard> creditCardRepository,
    IRepository<CreditChargeCategory> chargeCategoryRepository,
    IRepository<CreditChargePerson> chargePersonRepository,
    ICreditChargeCardBillingService cardBillingService,
    IUnitOfWork unitOfWork,
    IValidationPipelineOrchestrator validation
) : ICreditChargeService, IAutoTransient
{
    public async Task<CreditChargeOutput> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await chargeRepository
            .Include(c => c.CreditChargeCategories)
            .Include(c => c.CreditChargePeople)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        return entity != null ? new CreditChargeOutput(entity) : null;
    }

    public async Task<PagedOutput<CreditChargeOutput>> GetList(CreditChargeGetListInput input,
        CancellationToken cancellationToken = default)
    {
        return await chargeRepository
            .Include(c => c.CreditChargeCategories)
            .Include(c => c.CreditChargePeople)
            .WhereIf(input.CreditCardIds.Any(), c => input.CreditCardIds.Contains(c.CreditCardId))
            .WhereIf(input.DateFrom.HasValue, c => c.Date >= input.DateFrom)
            .WhereIf(input.DateTo.HasValue, c => c.Date <= input.DateTo)
            
            .WhereIf(input.CategoryIds.Any() && input.CategoryOperator == MultiplyFilterOperator.And, c =>
                input.CategoryIds.All(id => c.CreditChargeCategories.Any(cat => cat.TitleCategoryId == id)))
            .WhereIf(input.CategoryIds.Any() && input.CategoryOperator == MultiplyFilterOperator.Or,
                c => c.CreditChargeCategories.Any(cat => input.CategoryIds.Contains(cat.TitleCategoryId)))
            
            .WhereIf(input.PersonIds.Any() && input.PersonOperator == MultiplyFilterOperator.And, c =>
                input.PersonIds.All(id => c.CreditChargePeople.Any(p => p.PersonId == id)))
            .WhereIf(input.PersonIds.Any() && input.PersonOperator == MultiplyFilterOperator.Or,
                c => c.CreditChargePeople.Any(p => input.PersonIds.Contains(p.PersonId)))
            
            .OrderByDescending(c => c.Date)
            .ApplyFilterAndSorter(input)
            .Select(n => new CreditChargeOutput(n))
            .ToPagedResult(input, cancellationToken);
    }

    public async Task<ValidationResultDto<CreditChargeOutput, CreditChargeCreateOrUpdateErrorCode>> Create(CreditChargeInput input,
        bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateInput<CreditChargeOutput>(input, null, cancellationToken);
        if (!validationResult.Success) return validationResult;

        var creditCard = await creditCardRepository.FirstAsync(cc => cc.Id == input.CreditCardId, cancellationToken);
        var charge = new CreditCharge(
            input.Value,
            input.Description,
            input.Date,
            input.NumberOfInstallments,
            input.CreditCardId
        );

        await using var scope = await unitOfWork.BeginTransactionAsync(cancellationToken);
        
        await chargeRepository.AddAsync(charge, autoSave: false, cancellationToken);
        
        // Sync categories
        var categoriesToSync = input.CreditChargeCategoriesIds
            .Select(catId => new CreditChargeCategory(catId, charge.Id))
            .ToList();
        if (categoriesToSync.Any())
            await chargeCategoryRepository.AddRangeAsync(categoriesToSync, autoSave: false, cancellationToken);

        // Sync people
        var peopleToSync = input.CreditChargePeople
            .Select(p => new CreditChargePerson(charge.Id, p))
            .ToList();
        if (peopleToSync.Any())
            await chargePersonRepository.AddRangeAsync(peopleToSync, autoSave: false, cancellationToken);

        // Process CardBilling and Installments
        await cardBillingService.ReprocessCardBillingForCharge(charge, creditCard, autoSave: false, cancellationToken);
        
        if (autoSave) await scope.CompleteAsync(cancellationToken);

        validationResult.Data = new CreditChargeOutput(charge);
        return validationResult;
    }

    public async Task<ValidationResultDto<bool, CreditChargeCreateOrUpdateErrorCode>> Update(Guid id, CreditChargeInput input,
        bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateInput<bool>(input, id, cancellationToken);
        if (!validationResult.Success) return validationResult;

        var charge = await chargeRepository
            .Include(c => c.CreditChargeCategories)
            .Include(c => c.CreditChargePeople)
            .FirstAsync(c => c.Id == id, cancellationToken);

        var creditCard = await creditCardRepository.FirstAsync(cc => cc.Id == input.CreditCardId, cancellationToken);

        await using (var scope = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            // Update basic fields
            charge.Update(input.Value, input.Description, input.Date, input.NumberOfInstallments, input.CreditCardId);

            await chargeRepository.UpdateAsync(charge, autoSave: false, cancellationToken);

            // Sync categories
            var categoriesToRemove = charge.SyncCategoriesAndReturnToRemove(input.CreditChargeCategoriesIds);
            foreach (var category in categoriesToRemove)
            {
                await chargeCategoryRepository.DeleteAsync(category, autoSave: false, cancellationToken);
            }

            // Sync people
            var peopleToRemove = charge.SyncPeopleAndReturnToRemove(input.CreditChargePeople);
            foreach (var person in peopleToRemove)
            {
                await chargePersonRepository.DeleteAsync(person, autoSave: false, cancellationToken);
            }

            // Reprocess CardBilling and Installments
            await cardBillingService.ReprocessCardBillingAfterUpdate(charge, creditCard, autoSave: false, cancellationToken);
            
            if (autoSave) await scope.CompleteAsync(cancellationToken);
        }

        return validationResult.WithSuccess(true);
    }

    public async Task<ValidationResultDto<bool, CreditChargeDeleteErrorCode>> Delete(Guid id, bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validation.Validate<Guid, CreditChargeDeleteErrorCode>(id, null, cancellationToken);
        var validationResultDto = validationResult.ToValidationResult<bool, CreditChargeDeleteErrorCode>();
        if (!validationResultDto.Success) return validationResultDto;

        var charge = await chargeRepository.FirstAsync(c => c.Id == id, cancellationToken);
        var creditCard = await creditCardRepository.FirstAsync(cc => cc.Id == charge.CreditCardId, cancellationToken);

        await using (var scope = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            // Reprocess CardBilling before deletion
            await cardBillingService.ReprocessCardBillingAfterDelete(charge, creditCard, autoSave: false, cancellationToken);

            // Delete the charge
            await chargeRepository.DeleteAsync(charge, autoSave: false, cancellationToken);
            
            if (autoSave) await scope.CompleteAsync(cancellationToken);
        }

        return validationResultDto.WithSuccess(true);
    }

    private async Task<ValidationResultDto<TSuccess, CreditChargeCreateOrUpdateErrorCode>> ValidateInput<TSuccess>(
        CreditChargeInput input, Guid? editingId = null, CancellationToken cancellationToken = default)
    {
        var validationResult =
            await validation.Validate<CreditChargeInput, CreditChargeCreateOrUpdateErrorCode>(input, editingId, cancellationToken);
        return validationResult.ToValidationResult<TSuccess, CreditChargeCreateOrUpdateErrorCode>();
    }
}





