using PesaScope.Core.Models;

namespace PesaScope.App.Services.Interfaces;

public interface IBudgetNotificationService
{
    Task CheckCategoryBudgetAsync(int categoryId, decimal spent, Budget budget);
    Task CheckOverallBudgetAsync(decimal spent, OverallBudget overall);
}