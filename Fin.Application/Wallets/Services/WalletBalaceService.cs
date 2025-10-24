using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Extensions;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.UnitOfWorks;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Wallets.Services;

public interface IWalletBalanceService
{
    public Task<decimal?> GetBalanceAt(Guid walletId, DateTime dateTime, CancellationToken cancellationToken = default);
    public Task<decimal?> GetBalanceNow(Guid walletId, CancellationToken cancellationToken = default);
    public Task ReprocessBalance(Guid walletId, decimal newInitialBalance, bool autoSave = false , CancellationToken cancellationToken = default);
    public Task ReprocessBalance(Wallet wallet, bool autoSave = false , CancellationToken cancellationToken = default);
    public Task ReprocessBalance(List<Title> titles, decimal newInitialBalance, bool autoSave = false, CancellationToken cancellationToken = default);
    public Task ReprocessBalanceFrom(Title title, bool autoSave = false, CancellationToken cancellationToken = default);
    public Task ReprocessBalanceFrom(Guid titleId, bool autoSave = false, CancellationToken cancellationToken = default);
}

public class WalletBalanceService(
    IRepository<Wallet> walletRepository,
    IRepository<Title> titleRepository,
    IDateTimeProvider  dateTimeProvider,
    IUnitOfWork unitOfWork
    ): IWalletBalanceService, IAutoTransient
{
    public async Task<decimal?> GetBalanceAt(Guid walletId, DateTime dateTime, CancellationToken cancellationToken = default)
    {
        var wallet = await walletRepository.Query(tracking: false)
            .Include(wallet => wallet.Titles)
            .FirstOrDefaultAsync(wallet => wallet.Id == walletId, cancellationToken);
        return wallet?.CalculateBalanceAt(dateTime);
    }

    public Task<decimal?> GetBalanceNow(Guid walletId, CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow();
        return GetBalanceAt(walletId, now, cancellationToken);
    }

    public async Task ReprocessBalance(Guid walletId, decimal newInitialBalance, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var wallet = await walletRepository.Query()
            .Include(wallet => wallet.Titles)
            .FirstAsync(wallet => wallet.Id == walletId, cancellationToken);
        await ReprocessBalance(wallet.Titles.ToList(), newInitialBalance, autoSave, cancellationToken);
    }

    public async Task ReprocessBalance(Wallet wallet, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        await ReprocessBalance(wallet.Titles.ToList(), wallet.InitialBalance, autoSave, cancellationToken);
    }

    public async Task ReprocessBalance(List<Title> titles, decimal initialBalance, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var orderedTitles = titles.ApplyDefaultTitleOrder().ToList();
        var nextPreviousBalance = initialBalance;
        foreach (var title in orderedTitles)
        {
            title.PreviousBalance = nextPreviousBalance;
            nextPreviousBalance = title.ResultingBalance;
        }

        await using (await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            foreach (var title in orderedTitles)
                await titleRepository.UpdateAsync(title, cancellationToken);
            if (autoSave) await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    public async Task ReprocessBalanceFrom(Title fromTitle, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var titles = await titleRepository.Query(tracking: true)
            .Where(title => title.WalletId == fromTitle.WalletId)
            .Where(title => title.Date >= fromTitle.Date)
            .ApplyDefaultTitleOrder()
            .ToListAsync(cancellationToken);
        var fromTitleIndex =  titles.FindIndex(title => title.Id == fromTitle.Id);
        titles = titles.Skip(fromTitleIndex + 1).ToList();
        await ReprocessBalance(titles, fromTitle.ResultingBalance, autoSave, cancellationToken);
    }

    public async Task ReprocessBalanceFrom(Guid titleId, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var title = await titleRepository.Query(tracking: false)
            .FirstAsync(title => title.Id == titleId, cancellationToken);
        await ReprocessBalanceFrom(title,  autoSave, cancellationToken);
    }
}