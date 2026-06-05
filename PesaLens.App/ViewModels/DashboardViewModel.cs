using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using SkiaSharp;

namespace PesaLens.App.ViewModels;

public partial class CategorySpendItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount => $"Ksh {Amount:N0}";
}

public partial class DashboardViewModel : ObservableObject
{
    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly ITransactionRepository _transactions;
    private readonly ICategoryRepository _categories;

    // ── Week range (Mon 00:00 → Sun 23:59:59 of the current week) ────────────
    //    DayOfWeek is 0=Sun … 6=Sat; we anchor to Monday.
    private static (DateTime from, DateTime to) CurrentWeekRange()
    {
        var today = DateTime.Today;
        int daysSinceMon = ((int)today.DayOfWeek + 6) % 7; // Mon=0 … Sun=6
        var weekStart = today.AddDays(-daysSinceMon);
        var weekEnd = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
        return (weekStart, weekEnd);
    }

    public string PeriodLabel
    {
        get
        {
            var (from, to) = CurrentWeekRange();
            return $"{from:d MMM} – {to:d MMM yyyy}";
        }
    }

    // ── Summary ───────────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _moneyIn;
    [ObservableProperty] private decimal _moneyOut;
    [ObservableProperty] private decimal _netBalance;
    [ObservableProperty] private string _formattedMoneyIn = "Ksh 0";
    [ObservableProperty] private string _formattedMoneyOut = "Ksh 0";
    [ObservableProperty] private string _formattedNetBalance = "Ksh 0";

    // ── Chart ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private ISeries[] _spendingSeries = [];
    [ObservableProperty] private Axis[] _xAxes = [];
    [ObservableProperty] private Axis[] _yAxes = [];

    // ── Top categories ────────────────────────────────────────────────────────
    [ObservableProperty] private List<CategorySpendItem> _topCategories = [];

    // ── Recent transactions ───────────────────────────────────────────────────
    [ObservableProperty] private List<Transaction> _recentTransactions = [];

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private bool _isRefreshing = false;
    [ObservableProperty] private string _lastSynced = "Never";

    // ── Constructor ───────────────────────────────────────────────────────────
    public DashboardViewModel(ITransactionRepository transactions, ICategoryRepository categories)
    {
        _transactions = transactions;
        _categories = categories;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await RefreshDashboardAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await RefreshDashboardAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private static async Task NavigateToTransactionsAsync() =>
        await Shell.Current.GoToAsync("//TransactionsPage");

    // ── Core refresh logic ────────────────────────────────────────────────────

    private async Task RefreshDashboardAsync()
    {
        var (from, to) = CurrentWeekRange();

        await Task.WhenAll(
            LoadSummaryAsync(from, to),
            LoadChartAsync(from, to),
            LoadTopCategoriesAsync(from, to),
            LoadRecentTransactionsAsync()
        );

        LastSynced = $"Updated {DateTime.Now:h:mm tt}";
    }

    private async Task LoadSummaryAsync(DateTime from, DateTime to)
    {
        var inTask = _transactions.GetTotalReceivedAsync(from, to);
        var outTask = _transactions.GetTotalSpentAsync(from, to);

        await Task.WhenAll(inTask, outTask);

        MoneyIn = inTask.Result;
        MoneyOut = outTask.Result;
        NetBalance = MoneyIn - MoneyOut;

        FormattedMoneyIn = $"Ksh {MoneyIn:N0}";
        FormattedMoneyOut = $"Ksh {MoneyOut:N0}";
        FormattedNetBalance = $"Ksh {NetBalance:N0}";
    }

    private async Task LoadChartAsync(DateTime from, DateTime to)
    {
        var daily = await _transactions.GetDailySpendingAsync(from, to);

        // Always exactly 7 entries: Mon → Sun
        var labels = new string[7];
        var values = new double[7];
        var today = DateTime.Today;

        for (int i = 0; i < 7; i++)
        {
            var day = from.Date.AddDays(i);
            labels[i] = day.ToString("ddd")[..1];  // M, T, W …
            values[i] = daily.TryGetValue(day, out var amt) ? (double)amt : 0d;
        }

        // Subtle grid line colour — matches OutlineVariant from the design tokens
        var gridLinePaint = new SolidColorPaint(new SKColor(0xD6, 0xE2, 0xDC, 0x99)) // #D6E2DC @ 60%
        {
            StrokeThickness = 0.8f
        };

        // Muted label colour — matches OnSurfaceVariant
        var labelPaint = new SolidColorPaint(new SKColor(0x4B, 0x5F, 0x56));

        SpendingSeries =
        [
            new ColumnSeries<double>
            {
                Values                      = values,
                Fill                        = new SolidColorPaint(SKColor.Parse("#1A8C62")),
                Stroke                      = null,
                //IgnoresBarVisualTransitions = false,
                MaxBarWidth                 = 40,
                Rx                          = 6,
                Ry                          = 6,
                Name                        = "Daily spend",
                DataLabelsPaint             = null,
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels          = labels,
                TextSize        = 11,
                LabelsPaint     = labelPaint,
                SeparatorsPaint = null,
                TicksPaint      = null,
            }
        ];

        YAxes =
        [
            new Axis
            {
                TextSize        = 10,
                LabelsPaint     = labelPaint,
                SeparatorsPaint = gridLinePaint,
                TicksPaint      = null,
                Labeler         = val => val == 0 ? "0" : $"Ksh {val / 1000:0}k",
            }
        ];
    }

    private async Task LoadTopCategoriesAsync(DateTime from, DateTime to)
    {
        var spendByCategory = await _transactions.GetSpendingByCategoryAsync(from, to);
        var allCategories = await _categories.GetAllAsync();

        var catMap = allCategories.ToDictionary(c => c.Id);

        var top3 = spendByCategory
            .Where(kvp => catMap.ContainsKey(kvp.Key))
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => new CategorySpendItem
            {
                Name = catMap[kvp.Key].Name,
                Icon = catMap[kvp.Key].Icon,
                Color = catMap[kvp.Key].Color,
                Amount = kvp.Value
            })
            .ToList();

        TopCategories = top3;
    }

    private async Task LoadRecentTransactionsAsync()
    {
        RecentTransactions = await _transactions.GetRecentAsync(5);
    }
}