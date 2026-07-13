using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories;

public class BudgetRepository(DatabaseService databaseService)
    : BaseRepository<Budget>(databaseService), IBudgetRepository
{
    public Task<Budget?> GetByCategoryAsync(int categoryId) =>
        _db.Table<Budget>()
           .Where(b => b.CategoryId == categoryId)
           .FirstOrDefaultAsync()!;

    public Task<List<Budget>> GetAllWithCategoryAsync() =>
        _db.Table<Budget>().ToListAsync();

    public async Task<int> UpsertAsync(Budget budget)
    {
        var existing = await GetByCategoryAsync(budget.CategoryId);

        if (existing is null)
            return await _db.InsertAsync(budget);

        budget.Id = existing.Id;
        return await _db.UpdateAsync(budget);
    }

    public async Task<int> SetNotificationsEnabledAsync(int budgetId, bool enabled)
    {
        var budget = await _db.FindAsync<Budget>(budgetId);
        if (budget is null) return 0;

        budget.NotificationsEnabled = enabled;
        return await _db.UpdateAsync(budget);
    }

    public async Task<List<Budget>> GetBudgetsDueForWarningAsync()
    {
        // Returns budgets with notifications enabled.
        // The service layer computes current spending and compares against the threshold.
        return await _db.Table<Budget>()
                        .Where(b => b.NotificationsEnabled)
                        .ToListAsync();
    }
}