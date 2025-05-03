namespace Fin.Infrastructure.Database.IRepositories;

public interface IRepository<T>
{
    IQueryable<T> Query { get; }
    Task AddAsync(T entity, bool autoSave = false);
    Task UpdateAsync(T entity, bool autoSave = false);
    Task DeleteAsync(T entity, bool autoSave = false);
    Task SaveChangesAsync();
}