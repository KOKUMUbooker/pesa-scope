using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;
using SQLite;

namespace PesaScope.App.Data.Repositories;

public class OverallBudgetRepository(DatabaseService databaseService) : IOverallBudgetRepository
{
    private readonly SQLiteAsyncConnection _db = databaseService.Connection;

    public Task<OverallBudget?> GetAsync() =>
        _db.Table<OverallBudget>().FirstOrDefaultAsync()!;

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