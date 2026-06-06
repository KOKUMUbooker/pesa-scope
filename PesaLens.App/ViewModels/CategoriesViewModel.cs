using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace PesaLens.App.ViewModels;

// ── Display models ────────────────────────────────────────────────────────────

public partial class CategoryRowItem : ObservableObject
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsSystemCategory { get; set; }
    public decimal Amount { get; set; }
    public double Percentage { get; set; }

    public string FormattedAmount => $"Ksh {Amount:N0}";
    public string FormattedPercentage => $"{Percentage:F1}%";

    /// <summary>Width ratio 0..1 for the progress-bar fill.</summary>
    public double BarWidth => Percentage / 100.0;
}

public partial class RuleRowItem : ObservableObject
{
    public int RuleId { get; set; }
    public string MatchValue { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public RuleType RuleType { get; set; }
    public bool IsEnabled { get; set; }

    public string RuleTypeLabel => RuleType switch
    {
        RuleType.PaybillNumber => "Paybill",
        RuleType.TillNumber => "Till",
        RuleType.MerchantName => "Merchant",
        RuleType.ContainsText => "Contains",
        RuleType.TransactionType => "Tx Type",
        _ => RuleType.ToString()
    };
}

// ── ViewModel ─────────────────────────────────────────────────────────────────

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactions;
    private readonly ICategoryRepository _categories;
    private readonly IAutoCategorizationRuleRepository _rules;

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _periodLabel = string.Empty;

    // ── Donut chart ───────────────────────────────────────────────────────────
    [ObservableProperty] private ISeries[] _donutSeries = [];
    [ObservableProperty] private string _totalSpendLabel = "Ksh 0";

    // ── Category list ─────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<CategoryRowItem> _categoryRows = [];

    // ── Selected category (for drill-down) ───────────────────────────────────
    [ObservableProperty] private CategoryRowItem? _selectedCategory;
    [ObservableProperty] private bool _showCategoryDetail;

    // ── Rules section ─────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<RuleRowItem> _ruleRows = [];

    [ObservableProperty] private bool _showRules;

    // ── Tab selection (0 = Breakdown, 1 = Rules) ──────────────────────────────
    [ObservableProperty] private int _selectedTab;

    // ── Constructor ───────────────────────────────────────────────────────────
    public CategoriesViewModel(
        ITransactionRepository transactions,
        ICategoryRepository categories,
        IAutoCategorizationRuleRepository rules)
    {
        _transactions = transactions;
        _categories = categories;
        _rules = rules;
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

    // ── Tab switching ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectTabAsync(string tabIndex)
    {
        SelectedTab = int.Parse(tabIndex);
        if (SelectedTab == 1 && RuleRows.Count == 0)
            await LoadRulesAsync();
    }

    // ── Category drill-down ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectCategoryAsync(CategoryRowItem item)
    {
        SelectedCategory = item;
        ShowCategoryDetail = true;

        // Navigate to filtered transactions page for this category
        // Reuse TransactionsPage via query parameter — adjust route as needed
        await Shell.Current.GoToAsync(
            "TransactionsPage",
            new Dictionary<string, object>
            {
                ["CategoryId"] = item.CategoryId,
                ["CategoryName"] = item.Name
            });
    }

    [RelayCommand]
    private static async Task NavigateToAddCategoryAsync()
    {
        await Shell.Current.GoToAsync(
            nameof(PesaLens.App.Views.Categories.EditCategoryPage),
            new Dictionary<string, object> { ["CategoryId"] = 0 });
    }

    [RelayCommand]
    private void DismissCategoryDetail() => ShowCategoryDetail = false;

    // ── Rule toggle ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleRuleAsync(RuleRowItem row)
    {
        row.IsEnabled = !row.IsEnabled;
        // Persist via raw SQL — fastest path without a dedicated method
        var rule = await _rules.GetByIdAsync(row.RuleId);
        if (rule is null) return;
        rule.IsEnabled = row.IsEnabled;
        await _rules.UpdateAsync(rule);
        // row.OnPropertyChanged(nameof(RuleRowItem.IsEnabled));
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(RuleRowItem row)
    {
        await _rules.DeleteByIdAsync(row.RuleId);
        RuleRows.Remove(row);
    }

    // ── Core data loading ─────────────────────────────────────────────────────

    private async Task RefreshAllAsync()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        PeriodLabel = $"{monthStart:MMMM yyyy}";

        await Task.WhenAll(
            LoadBreakdownAsync(monthStart, monthEnd),
            SelectedTab == 1 ? LoadRulesAsync() : Task.CompletedTask
        );
    }

    private async Task LoadBreakdownAsync(DateTime from, DateTime to)
    {
        var spendByCategory = await _transactions.GetSpendingByCategoryAsync(from, to);
        var allCategories = await _categories.GetAllAsync();

        var catMap = allCategories.ToDictionary(c => c.Id);
        var total = spendByCategory.Values.Sum();

        TotalSpendLabel = $"Ksh {total:N0}";

        // Build display rows, sorted descending
        var rows = spendByCategory
            .Where(kvp => catMap.ContainsKey(kvp.Key) && kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp =>
            {
                var cat = catMap[kvp.Key];
                return new CategoryRowItem
                {
                    CategoryId = cat.Id,
                    Name = cat.Name,
                    Icon = cat.Icon,
                    Color = cat.Color,
                    IsSystemCategory = cat.IsSystemCategory,
                    Amount = kvp.Value,
                    Percentage = total > 0 ? (double)(kvp.Value / total * 100) : 0,
                };
            })
            .ToList();

        CategoryRows = new ObservableCollection<CategoryRowItem>(rows);

        // ── Build donut series ────────────────────────────────────────────
        var pieSeries = rows.Select(r =>
        {
            var fill = TryParseColor(r.Color, out var sk) ? sk : new SKColor(0x90, 0xA4, 0xAE);
            return (ISeries)new PieSeries<double>
            {
                Values = [(double)r.Amount],
                Name = r.Name,
                Fill = new SolidColorPaint(fill),
                Stroke = null,
                InnerRadius = 70,
                //MaxOuterRadius = 0.9,
                DataLabelsPaint = null,
            };
        }).ToArray();

        DonutSeries = pieSeries;
    }

    private async Task LoadRulesAsync()
    {
        var allRules = await _rules.GetEnabledOrderedByPriorityAsync();
        var allCategories = await _categories.GetAllAsync();
        // Also pull disabled; GetEnabledOrderedByPriorityAsync only returns enabled ones,
        // so fetch all via base GetAllAsync on repo then filter here
        var allRulesAll = await _rules.GetAllAsync();
        var catMap = allCategories.ToDictionary(c => c.Id);

        var rows = allRulesAll
            .OrderByDescending(r => r.Priority)
            .Select(r => new RuleRowItem
            {
                RuleId = r.Id,
                MatchValue = r.MatchValue,
                CategoryName = catMap.TryGetValue(r.CategoryId, out var c) ? c.Name : "Unknown",
                RuleType = r.RuleType,
                IsEnabled = r.IsEnabled,
            })
            .ToList();

        RuleRows = new ObservableCollection<RuleRowItem>(rows);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryParseColor(string hex, out SKColor color)
    {
        color = SKColor.Empty;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        hex = hex.TrimStart('#');
        if (hex.Length == 6 &&
            uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val))
        {
            color = new SKColor(
                (byte)((val >> 16) & 0xFF),
                (byte)((val >> 8) & 0xFF),
                (byte)(val & 0xFF));
            return true;
        }
        return false;
    }
}