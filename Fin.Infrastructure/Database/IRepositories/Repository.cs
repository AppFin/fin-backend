using Fin.Domain.Global.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database.IRepositories;

public class Repository<T> : IRepository<T> where T : class, IEntity
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public IQueryable<T> Query => _dbSet.AsNoTracking();

    public async Task AddAsync(T entity, bool autoSave = false)
    {
        await _dbSet.AddAsync(entity);
        if (autoSave)
            await SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity, bool autoSave = false)
    {
        _dbSet.Update(entity);
        if (autoSave)
            await SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity, bool autoSave = false)
    {
        _dbSet.Remove(entity);
        if (autoSave)
            await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    } 
}