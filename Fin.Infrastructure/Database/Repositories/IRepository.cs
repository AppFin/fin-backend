﻿namespace Fin.Infrastructure.Database.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query(bool tracking = true);
    Task AddAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<T> entities, bool autoSave = false, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
    Task UpdateAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken);
    Task DeleteAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    FinDbContext Context { get; }
}