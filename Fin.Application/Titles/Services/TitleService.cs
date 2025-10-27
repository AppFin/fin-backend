using Fin.Application.Globals.Dtos;
using Fin.Application.Titles.Dtos;
using Fin.Application.Titles.Enums;
using Fin.Application.Wallets.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Global.Enums;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Titles.Extensions;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.UnitOfWorks;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Services;

public interface ITitleService
{
    public Task<TitleOutput> Get(Guid id, CancellationToken cancellationToken = default);
    public Task<PagedOutput<TitleOutput>> GetList(TitleGetListInput input, CancellationToken cancellationToken = default);
    public Task<ValidationResultDto<TitleOutput, TitleCreateOrUpdateErrorCode>> Create(TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default);
    public Task<ValidationResultDto<bool, TitleCreateOrUpdateErrorCode>> Update(Guid id, TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default);
    public Task<ValidationResultDto<bool, TitleDeleteErrorCode>> Delete(Guid id, bool autoSave = false, CancellationToken cancellationToken = default);
}

public class TitleService(
    IRepository<Title> titleRepository,
    IRepository<TitleTitleCategory> titleTitleCategoryRepository,
    IWalletBalanceService balanceService,
    IUnitOfWork unitOfWork,
    IValidationPipelineOrchestrator validation
    ): ITitleService
{
    public async Task<TitleOutput> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await titleRepository.Query(false).FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        return entity != null ? new TitleOutput(entity) : null;
    }

    public async Task<PagedOutput<TitleOutput>> GetList(TitleGetListInput input, CancellationToken cancellationToken = default)
    {
        return await titleRepository.Query(false)
            .Include(title => title.TitleCategories)
            .WhereIf(input.Type.HasValue, n => n.Type == input.Type)
            .WhereIf(input.WalletIds.Any(), title => input.WalletIds.Contains(title.WalletId))
            .WhereIf(input.CategoryIds.Any() && input.CategoryOperator == MultiplyFilterOperator.And, title => 
                input.CategoryIds.All(id => title.TitleCategories.Any(c => c.Id == id)))
            .WhereIf(input.CategoryIds.Any() && input.CategoryOperator == MultiplyFilterOperator.Or, 
                title => title.TitleCategories.Any(titleCategory => input.CategoryIds.Contains(titleCategory.Id)))
            .ApplyDefaultTitleOrder()
            .ApplyFilterAndSorter(input)
            .Select(n => new TitleOutput(n))
            .ToPagedResult(input, cancellationToken);
    }

    public async Task<ValidationResultDto<TitleOutput, TitleCreateOrUpdateErrorCode>> Create(TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateInput<TitleOutput>(input, null, cancellationToken);
        if (!validationResult.Success) return validationResult;

        var previousBalance = await balanceService.GetBalanceAt(input.WalletId, input.Date, cancellationToken);
        var title = new Title(input, previousBalance);

        await using var scope = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await titleRepository.AddAsync(title, cancellationToken);
        await balanceService.ReprocessBalanceFrom(title, autoSave: false, cancellationToken);
        if (autoSave) await scope.CompleteAsync(cancellationToken);

        validationResult.Data = new TitleOutput(title);
        return validationResult;
    }

    public async Task<ValidationResultDto<bool, TitleCreateOrUpdateErrorCode>> Update(Guid id, TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateInput<bool>(input, id, cancellationToken);
        if (!validationResult.Success) return validationResult;
        
        var title = await titleRepository.Query(tracking: true)
            .FirstAsync(title => title.Id == id, cancellationToken);
        var mustReprocess = title.MustReprocess(input);

        var previousWalletId = title.WalletId;
        var previousDate = title.Date;
        var previousWalletPreviousBalance = title.PreviousBalance;
        
        var previousBalance = mustReprocess ? await balanceService.GetBalanceAt(input.WalletId, input.Date, cancellationToken) : title.PreviousBalance;
        if (mustReprocess && previousWalletId == input.WalletId && previousDate <= input.Date)
        {
            previousBalance += title.EffectiveValue * -1;
        }
        
        var categoriesToRemove = title.UpdateAndReturnToRemoveTitleCategories(input, previousBalance);
            
        await using (var scope = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            await titleRepository.UpdateAsync(title, cancellationToken);
            foreach (var category in categoriesToRemove)
            {
                await titleTitleCategoryRepository.DeleteAsync(category , cancellationToken);
            }

            if (mustReprocess)
            {
                var reprocessCurrentWalletFrom = previousWalletId != title.WalletId
                    ? title.Date
                    : previousDate > title.Date ? title.Date : previousDate;
            
                await balanceService.ReprocessBalance(title.WalletId, reprocessCurrentWalletFrom, title.ResultingBalance, autoSave = false, cancellationToken);
                if (previousWalletId != title.WalletId)
                {
                    previousWalletId = title.WalletId;
                    // TODO reprocess previous wallet
                }   
            }
        }

        validationResult.Data = true;
        return validationResult;
    }

    public Task<ValidationResultDto<bool, TitleDeleteErrorCode>> Delete(Guid id, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task<ValidationResultDto<TSuccess, TitleCreateOrUpdateErrorCode>> ValidateInput<TSuccess>(TitleInput input, Guid? editingId = null, CancellationToken cancellationToken = default)
    {
        var validationResult = await validation.Validate<TitleInput, TitleCreateOrUpdateErrorCode>(input, editingId, cancellationToken);
        return validationResult.ToValidationResult<TSuccess, TitleCreateOrUpdateErrorCode>();
    }
}