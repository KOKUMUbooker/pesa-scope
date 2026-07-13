using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories;

public class BudgetSnapshotRepository(DatabaseService databaseService)
    : BaseRepository<BudgetSnapshot>(databaseService), IBudgetSnapshotRepository
{
    public Task<BudgetSnapshot?> GetAsync(int year, int month, int? categoryId)
    {
        // categoryId null = overall budget row (stored with category_id = null)
        return categoryId is null
            ? (_db.Table<BudgetSnapshot>()
                 .Where(s => s.Year == year && s.Month == month && s.CategoryId == null)
                 .FirstOrDefaultAsync()) as Task<BudgetSnapshot?>
            : (_db.Table<BudgetSnapshot>()
                 .Where(s => s.Year == year && s.Month == month && s.CategoryId == categoryId)
                 .FirstOrDefaultAsync()) as Task<BudgetSnapshot?>;
    }

    public Task<List<BudgetSnapshot>> GetByMonthAsync(int year, int month) =>
        _db.Table<BudgetSnapshot>()
           .Where(s => s.Year == year && s.Month == month)
           .ToListAsync();

    public async Task<List<int>> GetAvailableYearsAsync()
    {
        // sqlite-net doesn't support .Select(s => s.Year).Distinct() in LINQ,
        // so we pull all snapshots and distinct in memory — the table is small
        var snapshots = await _db.Table<BudgetSnapshot>().ToListAsync();
        return snapshots
            .Select(s => s.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();
    }

    public async Task UpsertAsync(BudgetSnapshot snapshot)
    {
        var existing = await GetAsync(snapshot.Year, snapshot.Month, snapshot.CategoryId);

        if (existing is null)
        {
            await _db.InsertAsync(snapshot);
            return;
        }

        snapshot.Id = existing.Id;
        await _db.UpdateAsync(snapshot);
    }
}