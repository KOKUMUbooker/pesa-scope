using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using System.Collections.ObjectModel;

namespace PesaLens.App.ViewModels;

// ── Display model ─────────────────────────────────────────────────────────────

public partial class BudgetRowItem : ObservableObject
{
    public int BudgetId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public decimal Spent { get; set; }
    public decimal LastMonthSpent { get; set; }
    public bool NotificationsEnabled { get; set; }
    public int WarningThreshold { get; set; } = 80;

    // ── Derived ───────────────────────────────────────────────────────────────
    public double Progress => MonthlyLimit > 0 ? Math.Min((double)(Spent / MonthlyLimit), 1.0) : 0;
    public string FormattedSpent => $"Ksh {Spent:N0}";
    public string FormattedLimit => $"Ksh {MonthlyLimit:N0}";
    public string FormattedRemaining
    {
        get
        {
            var rem = MonthlyLimit - Spent;
            return rem >= 0 ? $"Ksh {rem:N0} left" : $"Ksh {Math.Abs(rem):N0} over";
        }
    }
    public bool IsOverBudget => Spent > MonthlyLimit;
    public bool IsWarning => !IsOverBudget && MonthlyLimit > 0 &&
                                        (double)(Spent / MonthlyLimit) * 100 >= WarningThreshold;

    /// <summary>Month-over-month delta. Positive = spent more than last month.</summary>
    public decimal MomDelta => Spent - LastMonthSpent;
    public string MomLabel
    {
        get
        {
            if (LastMonthSpent == 0) return "No data last month";
            var delta = MomDelta;
            return delta == 0
                ? "Same as last month"
                : $"{(delta > 0 ? "+" : "")}Ksh {delta:N0} vs last month";
        }
    }
    public bool MomIsHigher => MomDelta > 0;

    /// Percentage bar color key — "over", "warn", or "ok"
    public string StatusKey => IsOverBudget ? "over" : IsWarning ? "warn" : "ok";
}

// ── ViewModel ─────────────────────────────────────────────────────────────────

public partial class BudgetsViewModel : ObservableObject
{
    private readonly IBudgetRepository _budgets;
    private readonly IOverallBudgetRepository _overallBudget;
    private readonly ITransactionRepository _transactions;
    private readonly ICategoryRepository _categories;

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _periodLabel = string.Empty;

    // ── Overall budget ────────────────────────────────────────────────────────
    [ObservableProperty] private bool _hasOverallBudget;
    [ObservableProperty] private decimal _overallLimit;
    [ObservableProperty] private decimal _overallSpent;
    [ObservableProperty] private decimal _overallLastMonthSpent;
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private string _overallFormattedLimit = "—";
    [ObservableProperty] private string _overallFormattedSpent = "Ksh 0";
    [ObservableProperty] private string _overallFormattedRemaining = string.Empty;
    [ObservableProperty] private bool _overallIsOverBudget;
    [ObservableProperty] private bool _overallIsWarning;
    [ObservableProperty] private string _overallMomLabel = string.Empty;
    [ObservableProperty] private bool _overallMomIsHigher;

    // ── Category budgets ──────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<BudgetRowItem> _budgetRows = [];

    // ── Categories without a budget (for the Add sheet picker) ───────────────
    [ObservableProperty]
    private ObservableCollection<Category> _unbudgetedCategories = [];

    // ── Bottom-sheet: overall budget ──────────────────────────────────────────
    [ObservableProperty] private string _draftOverallLimit = string.Empty;
    [ObservableProperty] private bool _draftOverallNotifs = true;

    // ── Bottom-sheet: category budget ─────────────────────────────────────────
    [ObservableProperty] private Category? _draftCategory;
    [ObservableProperty] private string _draftCategoryLimit = string.Empty;
    [ObservableProperty] private bool _draftCategoryNotifs = true;
    [ObservableProperty] private int _draftWarningPct = 80;
    [ObservableProperty] private bool _isEditingExisting;   // true = edit, false = new
    [ObservableProperty] private int _editingBudgetId;

    // ── Validation ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _overallSheetError = string.Empty;
    [ObservableProperty] private string _categorySheetError = string.Empty;

    // ── Constructor ───────────────────────────────────────────────────────────
    public BudgetsViewModel(
        IBudgetRepository budgets,
        IOverallBudgetRepository overallBudget,
        ITransactionRepository transactions,
        ICategoryRepository categories)
    {
        _budgets = budgets;
        _overallBudget = overallBudget;
        _transactions = transactions;
        _categories = categories;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await RefreshAllAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try { await RefreshAllAsync(); }
        finally { IsRefreshing = false; }
    }

    // ── Open sheets: seed draft values ────────────────────────────────────────

    [RelayCommand]
    private void OpenOverallSheet()
    {
        OverallSheetError = string.Empty;
        DraftOverallLimit = HasOverallBudget ? OverallLimit.ToString("0") : string.Empty;
        DraftOverallNotifs = true;
    }

    [RelayCommand]
    private async Task OpenAddCategorySheetAsync()
    {
        CategorySheetError = string.Empty;
        IsEditingExisting = false;
        EditingBudgetId = 0;
        DraftCategory = null;
        DraftCategoryLimit = string.Empty;
        DraftCategoryNotifs = true;
        DraftWarningPct = 80;
        await RefreshUnbudgetedCategoriesAsync();
    }

    [RelayCommand]
    private async Task OpenEditCategorySheetAsync(BudgetRowItem row)
    {
        CategorySheetError = string.Empty;
        IsEditingExisting = true;
        EditingBudgetId = row.BudgetId;
        DraftCategoryLimit = row.MonthlyLimit.ToString("0");
        DraftCategoryNotifs = row.NotificationsEnabled;
        DraftWarningPct = row.WarningThreshold;

        // Seed the selected category
        var cat = await _categories.GetByIdAsync(row.CategoryId);
        DraftCategory = cat;
        await RefreshUnbudgetedCategoriesAsync();
    }

    // ── Save: overall budget ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveOverallBudgetAsync()
    {
        if (!decimal.TryParse(DraftOverallLimit, out var limit) || limit <= 0)
        {
            OverallSheetError = "Please enter a valid amount greater than 0.";
            return;
        }

        var record = new OverallBudget
        {
            MonthlyLimit = limit,
            NotificationsEnabled = DraftOverallNotifs,
            CreatedAt = DateTime.UtcNow,
        };
        await _overallBudget.UpsertAsync(record);
        await RefreshAllAsync();
    }

    [RelayCommand]
    private async Task DeleteOverallBudgetAsync()
    {
        await _overallBudget.DeleteAsync();
        HasOverallBudget = false;
        OverallFormattedLimit = "—";
        OverallFormattedRemaining = string.Empty;
        OverallProgress = 0;
    }

    // ── Save: category budget ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveCategoryBudgetAsync()
    {
        if (DraftCategory is null && !IsEditingExisting)
        {
            CategorySheetError = "Please select a category.";
            return;
        }
        if (!decimal.TryParse(DraftCategoryLimit, out var limit) || limit <= 0)
        {
            CategorySheetError = "Please enter a valid amount greater than 0.";
            return;
        }

        int catId = IsEditingExisting
            ? (await _budgets.GetByIdAsync(EditingBudgetId))!.CategoryId
            : DraftCategory!.Id;

        var budget = new Budget
        {
            CategoryId = catId,
            MonthlyLimit = limit,
            NotificationsEnabled = DraftCategoryNotifs,
            WarningThresholdPercent = DraftWarningPct,
            CreatedAt = DateTime.UtcNow,
        };

        if (IsEditingExisting) budget.Id = EditingBudgetId;

        await _budgets.UpsertAsync(budget);
        await RefreshAllAsync();
    }

    [RelayCommand]
    private async Task DeleteCategoryBudgetAsync(BudgetRowItem row)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Remove budget",
            $"Remove the budget for \"{row.CategoryName}\"?",
            "Remove", "Cancel");
        if (!confirmed) return;

        await _budgets.DeleteByIdAsync(row.BudgetId);
        BudgetRows.Remove(row);
    }

    // ── Core data refresh ─────────────────────────────────────────────────────

    private async Task RefreshAllAsync()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var lastStart = monthStart.AddMonths(-1);
        var lastEnd = monthStart.AddTicks(-1);

        PeriodLabel = $"{monthStart:MMMM yyyy}";

        await Task.WhenAll(
            LoadOverallAsync(monthStart, monthEnd, lastStart, lastEnd),
            LoadCategoryBudgetsAsync(monthStart, monthEnd, lastStart, lastEnd)
        );

        await RefreshUnbudgetedCategoriesAsync();
    }

    private async Task LoadOverallAsync(
        DateTime from, DateTime to, DateTime lastFrom, DateTime lastTo)
    {
        var overall = await _overallBudget.GetAsync();
        HasOverallBudget = overall is not null;

        var spent = await _transactions.GetTotalSpentAsync(from, to);
        var lastSpent = await _transactions.GetTotalSpentAsync(lastFrom, lastTo);

        OverallSpent = spent;
        OverallLastMonthSpent = lastSpent;

        if (overall is not null)
        {
            OverallLimit = overall.MonthlyLimit;
            OverallFormattedLimit = $"Ksh {overall.MonthlyLimit:N0}";
            OverallProgress = overall.MonthlyLimit > 0
                ? Math.Min((double)(spent / overall.MonthlyLimit), 1.0)
                : 0;
            var rem = overall.MonthlyLimit - spent;
            OverallFormattedRemaining = rem >= 0
                ? $"Ksh {rem:N0} remaining"
                : $"Ksh {Math.Abs(rem):N0} over budget";
            OverallIsOverBudget = spent > overall.MonthlyLimit;
            OverallIsWarning = !OverallIsOverBudget && overall.MonthlyLimit > 0 &&
                                  (double)(spent / overall.MonthlyLimit) * 100 >= 80;
        }

        OverallFormattedSpent = $"Ksh {spent:N0}";
        var delta = spent - lastSpent;
        OverallMomIsHigher = delta > 0;
        OverallMomLabel = lastSpent == 0
            ? "No data last month"
            : $"{(delta > 0 ? "+" : "")}Ksh {delta:N0} vs last month";
    }

    private async Task LoadCategoryBudgetsAsync(
        DateTime from, DateTime to, DateTime lastFrom, DateTime lastTo)
    {
        var allBudgets = await _budgets.GetAllWithCategoryAsync();
        var allCategories = await _categories.GetAllAsync();
        var catMap = allCategories.ToDictionary(c => c.Id);
        var spendThis = await _transactions.GetSpendingByCategoryAsync(from, to);
        var spendLast = await _transactions.GetSpendingByCategoryAsync(lastFrom, lastTo);

        var rows = allBudgets.Select(b =>
        {
            catMap.TryGetValue(b.CategoryId, out var cat);
            spendThis.TryGetValue(b.CategoryId, out var spent);
            spendLast.TryGetValue(b.CategoryId, out var lastSpent);

            return new BudgetRowItem
            {
                BudgetId = b.Id,
                CategoryId = b.CategoryId,
                CategoryName = cat?.Name ?? "Unknown",
                CategoryIcon = cat?.Icon ?? "help",
                CategoryColor = cat?.Color ?? "#90A4AE",
                MonthlyLimit = b.MonthlyLimit,
                Spent = spent,
                LastMonthSpent = lastSpent,
                NotificationsEnabled = b.NotificationsEnabled,
                WarningThreshold = b.WarningThresholdPercent,
            };
        })
        .OrderByDescending(r => r.Progress)
        .ToList();

        BudgetRows = new ObservableCollection<BudgetRowItem>(rows);
    }

    private async Task RefreshUnbudgetedCategoriesAsync()
    {
        var allCats = await _categories.GetAllAsync();
        var allBudgets = await _budgets.GetAllWithCategoryAsync();
        var budgetedIds = allBudgets.Select(b => b.CategoryId).ToHashSet();

        // In edit mode also include the current category so it stays in the picker
        if (IsEditingExisting && DraftCategory is not null)
            budgetedIds.Remove(DraftCategory.Id);

        UnbudgetedCategories = new ObservableCollection<Category>(
            allCats.Where(c => !budgetedIds.Contains(c.Id)));
    }
}