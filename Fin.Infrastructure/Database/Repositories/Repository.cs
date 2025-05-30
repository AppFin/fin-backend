﻿using Fin.Domain.Global.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database.Repositories;

public class Repository<T> : IRepository<T> where T : class, IEntity
{
    private readonly FinDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(FinDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public IQueryable<T> Query(bool tracking = true)
    {
        return tracking ? _dbSet : _dbSet.AsNoTracking();
    }

    public async Task<T> FindAsync(Guid entityId, bool tracking = true)
    {
        return await Query(tracking).FirstOrDefaultAsync(x => x.Id == entityId);
    }

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