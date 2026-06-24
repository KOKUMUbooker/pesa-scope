using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.Core.Models;

namespace PesaLens.App.ViewModels;

public partial class BudgetRow : ObservableObject
{
    public Category Category { get; init; } = null!;
    public Budget? Budget { get; init; }
    public decimal Spent { get; init; }
    public decimal SpentLastMonth { get; init; }
    public decimal Limit => Budget?.MonthlyLimit ?? 0;

    public double Progress => Limit > 0 ? Math.Min((double)(Spent / Limit), 1.0) : 0;
    public string FormattedSpent => $"Ksh {Spent:N0}";
    public string FormattedLimit => $"Ksh {Limit:N0}";
    public string FormattedLastMonth => $"Ksh {SpentLastMonth:N0}";
    public bool HasBudget => Budget is not null;
    public bool IsOverBudget => Limit > 0 && Spent > Limit;
    public bool IsWarning => Limit > 0 && !IsOverBudget &&
                             (double)(Spent / Limit) * 100 >= (Budget?.WarningThresholdPercent ?? 80);

    public Color ProgressColor => IsOverBudget
        ? Color.FromArgb("#C0392B")
        : IsWarning
            ? Color.FromArgb("#C98A00")
            : Color.FromArgb("#1A8C62");

    public string StatusLabel => IsOverBudget
        ? $"Over by Ksh {Spent - Limit:N0}"
        : IsWarning
            ? $"{(double)(Spent / Limit) * 100:0}% used"
            : $"Ksh {Limit - Spent:N0} remaining";

    public decimal LastMonthDelta => Spent - SpentLastMonth;
    public string LastMonthLabel => SpentLastMonth == 0
        ? "No data last month"
        : LastMonthDelta > 0
            ? $"↑ Ksh {LastMonthDelta:N0} more than last month"
            : LastMonthDelta < 0
                ? $"↓ Ksh {Math.Abs(LastMonthDelta):N0} less than last month"
                : "Same as last month";

    public Color LastMonthColor => LastMonthDelta > 0
        ? Color.FromArgb("#C0392B")
        : Color.FromArgb("#1A8C62");
}

public partial class BudgetsViewModel : ObservableObject
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IOverallBudgetRepository _overallBudgetRepo;

    // ── Page state ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private List<BudgetRow> _budgetRows = [];

    // ── Overall budget ────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _overallLimit;
    [ObservableProperty] private decimal _overallSpent;
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private string _formattedOverallSpent = "Ksh 0";
    [ObservableProperty] private string _formattedOverallLimit = "Ksh 0";
    [ObservableProperty] private bool _hasOverallBudget;
    [ObservableProperty] private Color _overallProgressColor = Color.FromArgb("#1A8C62");
    [ObservableProperty] private string _overallStatusLabel = string.Empty;

    // ── Add/Edit Budget sheet ─────────────────────────────────────────────────
    [ObservableProperty] private bool _isSheetOpen;
    [ObservableProperty] private BudgetRow? _editingRow;
    [ObservableProperty] private string _editLimitText = string.Empty;
    [ObservableProperty] private bool _editNotificationsEnabled = true;
    [ObservableProperty] private int _editWarningThreshold = 80;

    // ── Overall budget sheet ──────────────────────────────────────────────────
    [ObservableProperty] private bool _isOverallSheetOpen;
    [ObservableProperty] private string _editOverallLimitText = string.Empty;

    // Load states
    [ObservableProperty] private bool _isOverallBudgetLoading = true;
    [ObservableProperty] private bool _isCategoryBudgetsLoading = true;

    public BudgetsViewModel(
        IBudgetRepository budgetRepo,
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IOverallBudgetRepository overallBudgetRepo)
    {
        _budgetRepo = budgetRepo;
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _overallBudgetRepo = overallBudgetRepo;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await Task.WhenAll(LoadBudgetRowsAsync(), LoadOverallBudgetAsync());
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadBudgetRowsAsync()
    {
        IsCategoryBudgetsLoading = true;
        try
        {
            var (thisFrom, thisTo) = CurrentMonthRange();
            var (lastFrom, lastTo) = LastMonthRange();

            PeriodLabel = thisFrom.ToString("MMMM yyyy");

            var categories = await _categoryRepo.GetAllActiveAsync();
            var budgets = await _budgetRepo.GetAllWithCategoryAsync();
            var spendThis = await _transactionRepo.GetSpendingByCategoryAsync(thisFrom, thisTo);
            var spendLast = await _transactionRepo.GetSpendingByCategoryAsync(lastFrom, lastTo);
            var budgetMap = budgets.ToDictionary(b => b.CategoryId);

            BudgetRows = categories
                .Where(c => c.Name != "Uncategorized")
                .OrderByDescending(c => budgetMap.ContainsKey(c.Id))
                .ThenBy(c => c.Name)
                .Select(c => new BudgetRow
                {
                    Category = c,
                    Budget = budgetMap.GetValueOrDefault(c.Id),
                    Spent = spendThis.GetValueOrDefault(c.Id),
                    SpentLastMonth = spendLast.GetValueOrDefault(c.Id)
                })
                .ToList();
        }
        finally
        {
            IsCategoryBudgetsLoading = false;
        }
    }

    private async Task LoadOverallBudgetAsync()
    {
        IsOverallBudgetLoading = true;
        try
        {
            var overall = await _overallBudgetRepo.GetAsync();
            var (from, to) = CurrentMonthRange();
            OverallSpent = await _transactionRepo.GetTotalSpentAsync(from, to);

            HasOverallBudget = overall is not null;
            OverallLimit = overall?.MonthlyLimit ?? 0;
            FormattedOverallSpent = $"Ksh {OverallSpent:N0}";
            FormattedOverallLimit = overall is not null ? $"Ksh {OverallLimit:N0}" : "Not set";

            if (overall is not null && OverallLimit > 0)
            {
                OverallProgress = Math.Min((double)(OverallSpent / OverallLimit), 1.0);
                bool isOver = OverallSpent > OverallLimit;
                bool isWarn = !isOver && (double)(OverallSpent / OverallLimit) * 100 >= 80;

                OverallProgressColor = isOver
                    ? Color.FromArgb("#C0392B")
                    : isWarn
                        ? Color.FromArgb("#C98A00")
                        : Color.FromArgb("#1A8C62");

                OverallStatusLabel = isOver
                    ? $"Over by Ksh {OverallSpent - OverallLimit:N0}"
                    : $"Ksh {OverallLimit - OverallSpent:N0} remaining";
            }
            else
            {
                OverallProgress = 0;
                OverallStatusLabel = string.Empty;
            }
        }
        finally
        {
            IsOverallBudgetLoading = false;
        }
    }
    // ── Category budget sheet ─────────────────────────────────────────────────

    [RelayCommand]
    public void OpenEditBudget(BudgetRow row)
    {
        EditingRow = row;
        EditLimitText = row.Budget?.MonthlyLimit.ToString("0") ?? string.Empty;
        EditNotificationsEnabled = row.Budget?.NotificationsEnabled ?? true;
        EditWarningThreshold = row.Budget?.WarningThresholdPercent ?? 80;
        IsSheetOpen = true;
    }

    [RelayCommand]
    public async Task SaveBudgetAsync()
    {
        if (EditingRow is null) return;
        if (!decimal.TryParse(EditLimitText, out var limit) || limit <= 0) return;

        await _budgetRepo.UpsertAsync(new Budget
        {
            CategoryId = EditingRow.Category.Id,
            MonthlyLimit = limit,
            NotificationsEnabled = EditNotificationsEnabled,
            WarningThresholdPercent = EditWarningThreshold,
            CreatedAt = DateTime.UtcNow
        });

        IsSheetOpen = false;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteBudgetAsync(BudgetRow row)
    {
        if (row.Budget is null) return;
        await _budgetRepo.DeleteAsync(row.Budget);
        await LoadAsync();
    }

    // ── Overall budget sheet ──────────────────────────────────────────────────

    [RelayCommand]
    public void OpenOverallBudget()
    {
        EditOverallLimitText = OverallLimit > 0 ? OverallLimit.ToString("0") : string.Empty;
        IsOverallSheetOpen = true;
    }

    [RelayCommand]
    public async Task SaveOverallBudgetAsync()
    {
        if (!decimal.TryParse(EditOverallLimitText, out var limit) || limit <= 0) return;

        await _overallBudgetRepo.UpsertAsync(new OverallBudget
        {
            MonthlyLimit = limit,
            //UpdatedAt = DateTime.UtcNow,
        });

        IsOverallSheetOpen = false;
        await LoadOverallBudgetAsync();
    }

    [RelayCommand]
    public void CloseSheet()
    {
        IsSheetOpen = false;
        IsOverallSheetOpen = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DateTime from, DateTime to) CurrentMonthRange()
    {
        var today = DateTime.Today;
        var from = new DateTime(today.Year, today.Month, 1);
        var to = today;
        return (from, to);
    }

    private static (DateTime from, DateTime to) LastMonthRange()
    {
        var today = DateTime.Today;
        var from = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
        var to = from.AddMonths(1).AddDays(-1);
        return (from, to);
    }
}