using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.Services;

public class BudgetSnapshotService(
    IBudgetSnapshotRepository snapshotRepo,
    IBudgetRepository budgetRepo,
    ICategoryRepository categoryRepo,
    IOverallBudgetRepository overallBudgetRepo,
    ITransactionRepository transactionRepo) : IBudgetSnapshotService
{
    private readonly IBudgetSnapshotRepository _snapshotRepo = snapshotRepo;
    private readonly IBudgetRepository _budgetRepo = budgetRepo;
    private readonly ICategoryRepository _categoryRepo = categoryRepo;
    private readonly IOverallBudgetRepository _overallBudgetRepo = overallBudgetRepo;
    private readonly ITransactionRepository _transactionRepo = transactionRepo;

    public async Task TakeSnapshotAsync(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59); // last day of the month

        var spendingByCategory = await _transactionRepo.GetSpendingByCategoryAsync(from, to);
        var totalSpent = await _transactionRepo.GetTotalSpentAsync(from, to);

        await SnapshotOverallAsync(year, month, totalSpent);
        await SnapshotCategoriesAsync(year, month, spendingByCategory);
    }

    public async Task SnapshotPreviousMonthIfNeededAsync()
    {
        var prevMonth = DateTime.Today.AddMonths(-1);

        // Use the overall row as the sentinel — if it exists, the full
        // snapshot for that month was already taken
        var existing = await _snapshotRepo.GetAsync(
            prevMonth.Year, prevMonth.Month, categoryId: null);

        if (existing is not null) return;

        await TakeSnapshotAsync(prevMonth.Year, prevMonth.Month);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task SnapshotOverallAsync(int year, int month, decimal totalSpent)
    {
        var overall = await _overallBudgetRepo.GetAsync();

        // Snapshot even if no overall budget was set — records actual spending
        // so history is complete regardless of whether a limit existed
        var limit = overall?.MonthlyLimit ?? 0;

        await _snapshotRepo.UpsertAsync(new BudgetSnapshot
        {
            CategoryId = null,
            CategoryName = null,
            Year = year,
            Month = month,
            Limit = limit,
            Spent = totalSpent,
            WasExceeded = limit > 0 && totalSpent > limit,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task SnapshotCategoriesAsync(
        int year, int month, Dictionary<int, decimal> spendingByCategory)
    {
        var categories = await _categoryRepo.GetAllActiveAsync();
        var budgets = await _budgetRepo.GetAllWithCategoryAsync();
        var budgetMap = budgets.ToDictionary(b => b.CategoryId);

        // Snapshot every category that either had a budget set OR had spending —
        // this ensures history is complete even for unbudgeted categories
        var categoriesToSnapshot = categories
            .Where(c => c.Name != "Uncategorized" &&
                        (budgetMap.ContainsKey(c.Id) || spendingByCategory.ContainsKey(c.Id)));

        foreach (var category in categoriesToSnapshot)
        {
            var budget = budgetMap.GetValueOrDefault(category.Id);
            var spent = spendingByCategory.GetValueOrDefault(category.Id);
            var limit = budget?.MonthlyLimit ?? 0;

            await _snapshotRepo.UpsertAsync(new BudgetSnapshot
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Year = year,
                Month = month,
                Limit = limit,
                Spent = spent,
                WasExceeded = limit > 0 && spent > limit,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}