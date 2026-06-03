using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using SQLite;

namespace PesaLens.App.Repositories;

public class OverallBudgetRepository(SQLiteAsyncConnection db) : IOverallBudgetRepository
{
    private readonly SQLiteAsyncConnection _db = db;

    public Task<OverallBudget?> GetAsync() =>
        _db.Table<OverallBudget>().FirstOrDefaultAsync();

    public async Task<int> UpsertAsync(OverallBudget budget)
    {
        var existing = await GetAsync();

        if (existing is null)
            return await _db.InsertAsync(budget);

        budget.Id = existing.Id;
        return await _db.UpdateAsync(budget);
    }

    public async Task<int> DeleteAsync()
    {
        var existing = await GetAsync();
        return existing is null ? 0 : await _db.DeleteAsync(existing);
    }
}