using PesaLens.Core.Models;

namespace PesaLens.App.Services.Interfaces;

public interface IBudgetNotificationService
{
    Task CheckCategoryBudgetAsync(int categoryId, decimal spent, Budget budget);
    Task CheckOverallBudgetAsync(decimal spent, OverallBudget overall);
}