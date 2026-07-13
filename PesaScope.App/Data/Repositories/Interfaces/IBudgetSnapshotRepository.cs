using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories.Interfaces;

public interface IBudgetSnapshotRepository
{
    Task<BudgetSnapshot?> GetAsync(int year, int month, int? categoryId);
    Task<List<BudgetSnapshot>> GetByMonthAsync(int year, int month);
    Task<List<int>> GetAvailableYearsAsync();
    Task UpsertAsync(BudgetSnapshot snapshot);
}
