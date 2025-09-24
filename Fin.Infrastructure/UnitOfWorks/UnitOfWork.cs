using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fin.Infrastructure.UnitOfWorks;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    bool IsInTransaction();
}

public class UnitOfWork(FinDbContext context): IUnitOfWork, IAutoScoped
{
    private IDbContextTransaction _transaction;
    private bool _disposed;

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await context.Database.BeginTransactionAsync(cancellationToken);
        return _transaction;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var result = await context.SaveChangesAsync(cancellationToken);
        
        if (IsInTransaction())
            await _transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (IsInTransaction())
        {
            await _transaction?.RollbackAsync(cancellationToken)!;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken);
    
    public bool IsInTransaction()
    {
        return _transaction != null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        context.Dispose();
        _disposed = true;
    }

    public async  ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_transaction != null) await _transaction.DisposeAsync();
        await context.DisposeAsync();

        _disposed = true;
    }
}