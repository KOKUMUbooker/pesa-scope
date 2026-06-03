using PesaLens.App.Data.Repositories.Interfaces;
using SQLite;

namespace PesaLens.App.Data.Repositories;

/// <summary>
/// Generic base repository wiring up common CRUD operations against SQLiteAsyncConnection.
/// All entity-specific repositories extend this and inject the same shared connection.
/// </summary>
public abstract class BaseRepository<T>(SQLiteAsyncConnection db) : IRepository<T>
    where T : class, new()
{
    protected readonly SQLiteAsyncConnection _db = db;

    public Task<List<T>> GetAllAsync() =>
        _db.Table<T>().ToListAsync();

    public Task<T?> GetByIdAsync(int id) =>
        _db.FindAsync<T?>(id);

    public Task<int> InsertAsync(T entity) =>
        _db.InsertAsync(entity);

    public Task<int> UpdateAsync(T entity) =>
        _db.UpdateAsync(entity);

    public Task<int> DeleteAsync(T entity) =>
        _db.DeleteAsync(entity);

    public async Task<int> DeleteByIdAsync(int id)
    {
        var entity = await _db.FindAsync<T>(id);
        return entity is null ? 0 : await _db.DeleteAsync(entity);
    }
}