using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly FinDbContext _context;
    private readonly DbSet<T> _dbSet;

    public FinDbContext Context => _context;

    public Repository(FinDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public Type ElementType => _dbSet.AsQueryable().ElementType;
    public Expression Expression => _dbSet.AsQueryable().Expression;
    public IQueryProvider Provider => _dbSet.AsQueryable().Provider;

    public IEnumerator<T> GetEnumerator() => _dbSet.AsQueryable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IQueryable<T> AsNoTracking() => _dbSet.AsNoTracking();

    public async Task<T?> FindAsync(object keyValue, CancellationToken cancellationToken = default) =>
        await _dbSet.FindAsync(new[] { keyValue }, cancellationToken);

    public async Task<T?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default) =>
        await _dbSet.FindAsync(keyValues, cancellationToken);

    public IQueryable<T> Query(bool tracking = true)
    {
        return tracking ? _dbSet : _dbSet.AsNoTracking();
    }

    public async Task AddAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        if (autoSave)
            await SaveChangesAsync(cancellationToken);
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken)
    {
        return AddAsync(entity, false, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        if (autoSave)
            await SaveChangesAsync(cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        return AddRangeAsync(entities, false, cancellationToken);
    }

    public async Task UpdateAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        if (autoSave)
            await SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken)
    {
        return UpdateAsync(entity, false, cancellationToken);
    }

    public async Task DeleteAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        if (autoSave)
            await SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken)
    {
        return DeleteAsync(entity, false, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}