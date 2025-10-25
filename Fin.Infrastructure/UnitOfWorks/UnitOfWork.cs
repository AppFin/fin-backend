using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fin.Infrastructure.UnitOfWorks;

public interface IUnitOfWork
{
    Task<UnitOfWork.ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
    bool IsInTransaction();
}

public class UnitOfWork(FinDbContext context) : IUnitOfWork, IAutoScoped
{
    private readonly FinDbContext _context = context;

    private IDbContextTransaction _transaction;
    private int _transactionDepth;

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transactionDepth == 0 && _transaction == null)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
        }

        _transactionDepth++;
        
        return new TransactionScope(this, _transaction);
    }
    
    public bool IsInTransaction()
    {
        return _transaction != null;
    }
    
    public interface ITransactionScope: IAsyncDisposable
    {
        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
    
    private class TransactionScope(UnitOfWork uow, IDbContextTransaction transaction) : ITransactionScope
    {
        private bool _completed;

        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            _completed = true;
            return await uow._context.SaveChangesAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (uow.IsInTransaction())
            {
                await transaction.RollbackAsync(cancellationToken)!;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await uow._context.SaveChangesAsync(cancellationToken);
        
        public async ValueTask DisposeAsync()
        {
            uow._transactionDepth--;
            
            if (uow._transactionDepth == 0 && uow.IsInTransaction())
            {
                if (_completed)
                    await transaction.CommitAsync();
                else
                    await transaction.RollbackAsync();
                    
                await transaction.DisposeAsync();
            }
        }
    }
}