using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.Core.Models;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Data;

namespace PesaLens.App.ViewModels;

public partial class CategorySpendRow : ObservableObject
{
    public Category Category { get; init; } = null!;
    public decimal Amount { get; init; }
    public double Percentage { get; init; }
    public string FormattedAmount => $"Ksh {Amount:N0}";
    public string FormattedPercentage => $"{Percentage:0.#}%";
    public double ProgressValue => Percentage / 100.0;
    public Color ChartColor { get; init; } = Colors.Gray;
}

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IAutoCategorizationRuleRepository _rulesRepo;

    // Sentinel used to represent "no filter" in the category filter picker.
    private static readonly Category AllCategoriesOption = new()
    {
        Id = 0,
        Name = "All Categories"
    };

    // Unfiltered rules as loaded from the repo. Rules (below) is the filtered/displayed view.
    private List<AutoCategorizationRule> _allRules = [];

    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private List<CategorySpendRow> _categoryRows = [];
    [ObservableProperty] private ObservableCollection<AutoCategorizationRule> _rules = [];
    [ObservableProperty] private bool _isBusy;

    // Individual loading states for each section
    [ObservableProperty] private bool _isChartLoading;
    [ObservableProperty] private bool _isCategoriesLoading;
    [ObservableProperty] private bool _isRulesLoading;
    [ObservableProperty] private bool _isChartEmpty;

    [ObservableProperty] private bool _isRefreshing;

    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private CategorySpendRow? _selectedCategory;

    // ── Rules search / filter state ──────────────────────────────────────────
    [ObservableProperty] private string _ruleSearchText = string.Empty;
    [ObservableProperty] private Category? _ruleFilterCategory;
    [ObservableProperty] private List<Category> _ruleFilterCategories = [AllCategoriesOption];
    [ObservableProperty] private string _rulesEmptyMessage = "No rules yet. Tap '+ Add rule' to create one.";

    // ── Add/Edit Category sheet state ────────────────────────────────────────
    [ObservableProperty] private bool _isSheetOpen;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editIcon = "label";
    [ObservableProperty] private string _editColor = "#1A8C62";
    [ObservableProperty] private Category? _editingCategory;

    // ── Add Rule sheet state ──────────────────────────────────────────────────
    [ObservableProperty] private bool _isRuleSheetOpen;
    [ObservableProperty] private string _ruleMatchValue = string.Empty;
    [ObservableProperty] private RuleType _ruleType = RuleType.ContainsText;
    [ObservableProperty] private Category? _ruleTargetCategory;
    [ObservableProperty] private AutoCategorizationRule? _editingRule;

    public List<RuleType> RuleTypes { get; } = Enum.GetValues<RuleType>().ToList();

    public CategoriesViewModel(
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IAutoCategorizationRuleRepository rulesRepo)
    {
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _rulesRepo = rulesRepo;

        // Default the filter to "All Categories" so the picker has a sane initial selection.
        RuleFilterCategory = AllCategoriesOption;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // Load all sections in parallel with individual loading states
            var chartTask = LoadChartAsync();
            var categoriesTask = LoadCategoriesAsync();
            var rulesTask = LoadRulesAsync();

            await Task.WhenAll(chartTask, categoriesTask, rulesTask);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // ── Chart loading ─────────────────────────────────────────────────────────

    private async Task LoadChartAsync()
    {
        IsChartLoading = true;
        try
        {
            var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = DateTime.Today;
            PeriodLabel = from.ToString("MMMM yyyy");

            var categories = await _categoryRepo.GetAllActiveAsync();
            var spendMap = await _transactionRepo.GetSpendingByCategoryAsync(from, to);

            // Categories with spending first, then zero-spend alphabetically
            var ordered = categories
                .OrderByDescending(c => spendMap.GetValueOrDefault(c.Id))
                .ThenBy(c => c.Name)
                .ToList();

            decimal total = spendMap.Values.Sum();

            var pieSeries = new List<ISeries>();
            var categoryRows = new List<CategorySpendRow>();

            foreach (var cat in ordered)
            {
                var amount = spendMap.GetValueOrDefault(cat.Id);
                var color = ParseColor(cat.Color);
                var pct = total > 0 ? (double)(amount / total) * 100 : 0;

                categoryRows.Add(new CategorySpendRow
                {
                    Category = cat,
                    Amount = amount,
                    Percentage = pct,
                    ChartColor = Color.FromArgb(cat.Color)
                });

                // Only add to pie chart if there's actual spending
                if (amount > 0)
                {
                    pieSeries.Add(new PieSeries<double>
                    {
                        Values = [(double)amount],
                        Name = cat.Name,
                        Fill = new SolidColorPaint(color),
                        Stroke = null,
                        OuterRadiusOffset = 0,
                        MaxRadialColumnWidth = 28,
                        ToolTipLabelFormatter = p => $"{cat.Name}: Ksh {amount:N0}"
                    });
                }
            }

            Series = [.. pieSeries];
            CategoryRows = categoryRows;
            IsChartEmpty = total <= 0;
        }
        finally
        {
            IsChartLoading = false;
        }
    }

    // ── Categories loading (separate from chart) ─────────────────────────────

    private async Task LoadCategoriesAsync()
    {
        IsCategoriesLoading = true;
        try
        {
            await Task.CompletedTask; // Placeholder for future category-specific loading
        }
        finally
        {
            IsCategoriesLoading = false;
        }
    }

    // ── Rules loading ─────────────────────────────────────────────────────────
    private async Task LoadRulesAsync()
    {
        IsRulesLoading = true;
        try
        {
            var result = await _rulesRepo.GetEnabledOrderedByPriorityAsync();
            var categories = await _categoryRepo.GetAllAsync();
            var catMap = categories.ToDictionary(c => c.Id);

            foreach (var rule in result)
                rule.Category = catMap.GetValueOrDefault(rule.CategoryId);

            _allRules = result;

            // Build the filter dropdown: "All Categories" + every category that actually has a rule,
            // alphabetically. Re-built each load so newly added/removed categories stay in sync.
            RuleFilterCategories =
            [
                AllCategoriesOption,
                .. result
                    .Where(r => r.Category is not null)
                    .Select(r => r.Category!)
                    .DistinctBy(c => c.Id)
                    .OrderBy(c => c.Name)
            ];

            // Preserve the current filter selection if it still exists in the new list,
            // otherwise fall back to "All Categories".
            RuleFilterCategory = RuleFilterCategories
                .FirstOrDefault(c => c.Id == RuleFilterCategory?.Id) ?? AllCategoriesOption;

            ApplyRuleFilters();
        }
        finally
        {
            IsRulesLoading = false;
        }
    }

    partial void OnRuleSearchTextChanged(string value) => ApplyRuleFilters();

    partial void OnRuleFilterCategoryChanged(Category? value) => ApplyRuleFilters();

    private void ApplyRuleFilters()
    {
        IEnumerable<AutoCategorizationRule> filtered = _allRules;

        if (RuleFilterCategory is not null && RuleFilterCategory.Id != 0)
            filtered = filtered.Where(r => r.CategoryId == RuleFilterCategory.Id);

        if (!string.IsNullOrWhiteSpace(RuleSearchText))
        {
            var search = RuleSearchText.Trim();
            filtered = filtered.Where(r =>
                r.MatchValue.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.RuleType.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.Category?.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Rules = new ObservableCollection<AutoCategorizationRule>(filtered);

        RulesEmptyMessage = _allRules.Count == 0
            ? "No rules yet. Tap '+ Add rule' to create one."
            : "No rules match your search or filter.";
    }

    // ── Add / Edit category (4.4, 4.5) ───────────────────────────────────────

    [RelayCommand]
    public void OpenAddCategory()
    {
        EditingCategory = null;
        EditName = string.Empty;
        EditIcon = "label";
        EditColor = "#1A8C62";
        IsSheetOpen = true;
    }

    [RelayCommand]
    public void OpenEditCategory(CategorySpendRow row)
    {
        EditingCategory = row.Category;
        EditName = row.Category.Name;
        EditIcon = row.Category.Icon;
        EditColor = row.Category.Color;
        IsSheetOpen = true;
    }

    [RelayCommand]
    public async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName)) return;

        if (EditingCategory is null)
        {
            await _categoryRepo.InsertAsync(new Category
            {
                Name = EditName.Trim(),
                Icon = EditIcon,
                Color = EditColor,
                IsSystemCategory = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            EditingCategory.Name = EditName.Trim();
            EditingCategory.Icon = EditIcon;
            EditingCategory.Color = EditColor;
            await _categoryRepo.UpdateAsync(EditingCategory);
        }

        IsSheetOpen = false;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteCategoryAsync(CategorySpendRow row)
    {
        if (row.Category.IsSystemCategory) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Category",
            $"Transactions in '{row.Category.Name}' will be moved to Uncategorized, and any linked rules or budgets will be removed. This can't be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        await _categoryRepo.DeleteAndReassignAsync(row.Category.Id);
        await LoadAsync();
    }

    // ── Add / Edit rule (4.6) ─────────────────────────────────────────────────

    [RelayCommand]
    public void OpenAddRule()
    {
        EditingRule = null;
        RuleMatchValue = string.Empty;
        RuleType = RuleType.ContainsText;
        RuleTargetCategory = null;
        IsRuleSheetOpen = true;
    }

    [RelayCommand]
    public void OpenEditRule(AutoCategorizationRule rule)
    {
        EditingRule = rule;
        RuleMatchValue = rule.MatchValue;
        RuleType = rule.RuleType;
        IsRuleSheetOpen = true;
    }

    [RelayCommand]
    public async Task SaveRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(RuleMatchValue) || RuleTargetCategory is null) return;

        if (EditingRule is null)
        {
            await _rulesRepo.InsertAsync(new AutoCategorizationRule
            {
                RuleType = RuleType,
                MatchValue = RuleMatchValue.Trim(),
                CategoryId = RuleTargetCategory.Id,
                Priority = 5,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            EditingRule.RuleType = RuleType;
            EditingRule.MatchValue = RuleMatchValue.Trim();
            EditingRule.CategoryId = RuleTargetCategory!.Id;
            await _rulesRepo.UpdateAsync(EditingRule);
        }

        IsRuleSheetOpen = false;
        await LoadRulesAsync();
    }

    [RelayCommand]
    public async Task DeleteRuleAsync(AutoCategorizationRule rule)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Rule",
            $"Are you sure you want to delete this rule as this can't be undone ?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        await _rulesRepo.DeleteAsync(rule);
        await LoadRulesAsync();
    }

    [RelayCommand]
    public void CloseSheet()
    {
        IsSheetOpen = false;
        IsRuleSheetOpen = false;
    }

    [RelayCommand]
    public void ClearRuleFilters()
    {
        RuleSearchText = string.Empty;
        RuleFilterCategory = AllCategoriesOption;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SKColor ParseColor(string hex)
    {
        try { return SKColor.Parse(hex); }
        catch { return SKColor.Parse("#90A4AE"); }
    }
}