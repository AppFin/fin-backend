using Fin.Domain.Global.Interfaces;

namespace Fin.Infrastructure.Database.IRepositories;

public interface IRepository<T> where T : class, IEntity
{
    IQueryable<T> Query(bool tracking = true);
    Task<T> FindAsync(Guid entityId, bool tracking = true);
    Task AddAsync(T entity, bool autoSave = false);
    Task UpdateAsync(T entity, bool autoSave = false);
    Task DeleteAsync(T entity, bool autoSave = false);
    Task SaveChangesAsync();
}