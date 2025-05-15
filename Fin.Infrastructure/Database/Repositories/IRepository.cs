namespace Fin.Infrastructure.Database.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query(bool tracking = true);
    Task AddAsync(T entity, bool autoSave = false);
    Task UpdateAsync(T entity, bool autoSave = false);
    Task DeleteAsync(T entity, bool autoSave = false);
    Task SaveChangesAsync();
}