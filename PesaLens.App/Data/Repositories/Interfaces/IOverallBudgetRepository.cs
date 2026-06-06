using PesaLens.Core.Models;

namespace PesaLens.App.Data.Repositories.Interfaces;

public interface IOverallBudgetRepository
{
    /// <summary>
    /// Returns the single overall budget row. Null if the user has not set one yet.
    /// </summary>
    Task<OverallBudget?> GetAsync();

    /// <summary>
    /// Inserts or updates the overall budget (only one row ever exists).
    /// </summary>
    Task<int> UpsertAsync(OverallBudget budget);

    /// <summary>
    /// Removes the overall budget — user opts out of a global spending cap.
    /// </summary>
    Task<int> DeleteAsync();
}