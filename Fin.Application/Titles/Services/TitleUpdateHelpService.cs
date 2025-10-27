using Fin.Application.Wallets.Services;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Services;

public interface ITitleUpdateHelpService
{
    Task UpdateTitleAndCategories(
        Title title,
        TitleInput input,
        List<TitleTitleCategory> categoriesToRemove,
        CancellationToken cancellationToken);

    Task<UpdateTitleContext> PrepareUpdateContext(
        Title title,
        TitleInput input,
        bool mustReprocess,
        CancellationToken cancellationToken);

    Task<decimal> CalculatePreviousBalance(
        Title title,
        TitleInput input,
        CancellationToken cancellationToken);

    Task ReprocessAffectedWallets(
        Title title,
        UpdateTitleContext titleContext,
        bool autoSave,
        CancellationToken cancellationToken);

    Task ReprocessPreviousWallet(
        Title title,
        UpdateTitleContext titleContext,
        bool autoSave,
        CancellationToken cancellationToken);

    Task ReprocessCurrentWallet(
        Title title,
        UpdateTitleContext titleContext,
        bool autoSave,
        CancellationToken cancellationToken);

    Task<List<Title>> GetTitlesForReprocessing(
        Guid walletId,
        DateTime fromDate,
        Guid afterTitleId,
        CancellationToken cancellationToken);
}

public class TitleUpdateHelpService(
    IRepository<Title> titleRepository,
    IRepository<TitleTitleCategory> titleTitleCategoryRepository,
    IWalletBalanceService balanceService
): ITitleUpdateHelpService, IAutoTransient
{
    public async Task UpdateTitleAndCategories(
        Title title,
        TitleInput input,
        List<TitleTitleCategory> categoriesToRemove,
        CancellationToken cancellationToken)
    {
        await titleRepository.UpdateAsync(title, cancellationToken);
        foreach (var category in categoriesToRemove)
        {
            await titleTitleCategoryRepository.DeleteAsync(category, cancellationToken);
        }
    }

    public async Task<UpdateTitleContext> PrepareUpdateContext(
        Title title,
        TitleInput input,
        bool mustReprocess,
        CancellationToken cancellationToken)
    {
        var previousBalance = mustReprocess
            ? await CalculatePreviousBalance(title, input, cancellationToken)
            : title.PreviousBalance;

        var categoriesToRemove = title.UpdateAndReturnCategoriesToRemove(input, previousBalance);

        return new UpdateTitleContext(
            PreviousWalletId: title.WalletId,
            PreviousDate: title.Date,
            PreviousBalance: title.PreviousBalance,
            CategoriesToRemove: categoriesToRemove
        );
    }

    public async Task<decimal> CalculatePreviousBalance(
        Title title,
        TitleInput input,
        CancellationToken cancellationToken)
    {
        var balance = await balanceService.GetBalanceAt(input.WalletId, input.Date, cancellationToken);

        var isSameWallet = title.WalletId == input.WalletId;
        var shouldAdjustBalance = isSameWallet && title.Date <= input.Date;

        return shouldAdjustBalance
            ? balance - title.EffectiveValue
            : balance;
    }

    public async Task ReprocessAffectedWallets(
        Title title,
        UpdateTitleContext titleContext,
        bool autoSave,
        CancellationToken cancellationToken)
    {
        await ReprocessCurrentWallet(title, titleContext, autoSave, cancellationToken);

        if (titleContext.PreviousWalletId != title.WalletId)
        {
            await ReprocessPreviousWallet(title, titleContext, autoSave, cancellationToken);
        }
    }

    public async Task ReprocessPreviousWallet(
        Title title,
        UpdateTitleContext titleContext,
        bool autoSave,
        CancellationToken cancellationToken)
    {
        var titlesToReprocess = await GetTitlesForReprocessing(
            titleContext.PreviousWalletId,
            titleContext.PreviousDate,
            title.Id,
            cancellationToken);

        await balanceService.ReprocessBalance(
            titlesToReprocess,
            titleContext.PreviousBalance,
            autoSave,
            cancellationToken);
    }

    public async Task ReprocessCurrentWallet(
        Title title,
        UpdateTitleContext titleContext,
        bool autoSave,
        CancellationToken cancellationToken)
    {
        var walletChanged = titleContext.PreviousWalletId != title.WalletId;
        var reprocessFrom = walletChanged
            ? title.Date
            : titleContext.PreviousDate > title.Date
                ? title.Date
                : titleContext.PreviousDate;

        var titlesToReprocess = await GetTitlesForReprocessing(
            title.WalletId,
            reprocessFrom,
            title.Id,
            cancellationToken);

        await balanceService.ReprocessBalance(
            titlesToReprocess,
            title.ResultingBalance,
            autoSave,
            cancellationToken);
    }

    public async Task<List<Title>> GetTitlesForReprocessing(
        Guid walletId,
        DateTime fromDate,
        Guid afterTitleId,
        CancellationToken cancellationToken)
    {
        return await titleRepository.Query(tracking: true)
            .Where(t => t.WalletId == walletId)
            .Where(t => t.Date >= fromDate && t.Id > afterTitleId)
            .ToListAsync(cancellationToken);
    }
}

public record UpdateTitleContext(
    Guid PreviousWalletId,
    DateTime PreviousDate,
    decimal PreviousBalance,
    List<TitleTitleCategory> CategoriesToRemove
);

