using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace PesaScope.App.Services;

public class BudgetNotificationService(
    IAppSettingsRepository settingsRepo,
    IBudgetRepository budgetRepo,
    IOverallBudgetRepository overallBudgetRepo) : IBudgetNotificationService
{
    private readonly IAppSettingsRepository _settingsRepo = settingsRepo;
    private readonly IBudgetRepository _budgetRepo = budgetRepo;
    private readonly IOverallBudgetRepository _overallBudgetRepo = overallBudgetRepo;

    public async Task CheckCategoryBudgetAsync(int categoryId, decimal spent, Budget budget)
    {
        var settings = await _settingsRepo.GetAsync();
        if (settings is { BudgetNotificationsEnabled: false }) return;
        if (!budget.NotificationsEnabled) return;

        int thisMonth = int.Parse(DateTime.Today.ToString("yyyyMM"));
        double pct = (double)(spent / budget.MonthlyLimit) * 100;
        bool isOver = spent > budget.MonthlyLimit;
        bool isWarning = !isOver && pct >= budget.WarningThresholdPercent;

        if (isOver && budget.LastExceededNotifiedMonth != thisMonth)
        {
            await LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = 2000 + categoryId,
                Title = "Budget Exceeded 🔴",
                Description = $"You've gone over your {budget.Category?.Name} budget.",
            });
            budget.LastExceededNotifiedMonth = thisMonth;
            await _budgetRepo.UpsertAsync(budget);
        }
        else if (isWarning && budget.LastWarningNotifiedMonth != thisMonth)
        {
            await LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = 1000 + categoryId,
                Title = "Budget Warning 🟡",
                Description = $"{budget.Category?.Name}: {pct:0}% of your limit used."
            });
            budget.LastWarningNotifiedMonth = thisMonth;
            await _budgetRepo.UpsertAsync(budget);
        }
    }

    public async Task CheckOverallBudgetAsync(decimal spent, OverallBudget overall)
    {
        // Respect the global notification kill-switch
        var settings = await _settingsRepo.GetAsync();
        if (settings is { BudgetNotificationsEnabled: false }) return;

        // Overall budget has its own notifications toggle
        if (!overall.NotificationsEnabled) return;

        int thisMonth = int.Parse(DateTime.Today.ToString("yyyyMM"));
        double pct = (double)(spent / overall.MonthlyLimit) * 100;
        bool isOver = spent > overall.MonthlyLimit;

        // Overall budget uses a fixed 80% warning threshold since there's
        // no per-row WarningThresholdPercent on OverallBudget
        bool isWarning = !isOver && pct >= 80;

        if (isOver && overall.LastExceededNotifiedMonth != thisMonth)
        {
            await LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = 9002,
                Title = "Overall Budget Exceeded 🔴",
                Description = $"You've spent Ksh {spent:N0} of your Ksh {overall.MonthlyLimit:N0} monthly budget.",
            });
            overall.LastExceededNotifiedMonth = thisMonth;
            await _overallBudgetRepo.UpsertAsync(overall);
        }
        else if (isWarning && overall.LastWarningNotifiedMonth != thisMonth)
        {
            await LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = 9001,
                Title = "Overall Budget Warning 🟡",
                Description = $"You've used {pct:0}% of your monthly budget.",
            });
            overall.LastWarningNotifiedMonth = thisMonth;
            await _overallBudgetRepo.UpsertAsync(overall);
        }
    }
}