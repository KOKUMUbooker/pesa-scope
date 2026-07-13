using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories.Interfaces;

public interface IBudgetRepository : IRepository<Budget>
{
    /// <summary>
    /// Returns the budget for a specific category. Null if none is set.
    /// </summary>
    Task<Budget?> GetByCategoryAsync(int categoryId);

    /// <summary>
    /// Returns all budgets. Used to render the budgets overview page.
    /// </summary>
    Task<List<Budget>> GetAllWithCategoryAsync();

    /// <summary>
    /// Inserts a new budget or replaces the existing one for the same category.
    /// A category can only have one budget at a time.
    /// </summary>
    Task<int> UpsertAsync(Budget budget);

    /// <summary>
    /// Enables or disables notifications for a specific budget.
    /// </summary>
    Task<int> SetNotificationsEnabledAsync(int budgetId, bool enabled);

    /// <summary>
    /// Returns all budgets whose current spending has crossed the warning threshold
    /// but whose warning notification has not yet been sent.
    /// Used by NotificationService on sync completion.
    /// </summary>
    Task<List<Budget>> GetBudgetsDueForWarningAsync();
}