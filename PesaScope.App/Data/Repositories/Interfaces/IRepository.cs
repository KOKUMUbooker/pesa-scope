namespace PesaScope.App.Data.Repositories.Interfaces;

/// <summary>
/// Generic base repository interface. All entity-specific interfaces extend this.
/// </summary>
public interface IRepository<T> where T : class, new()
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<int> InsertAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(T entity);
    Task<int> DeleteByIdAsync(int id);
}