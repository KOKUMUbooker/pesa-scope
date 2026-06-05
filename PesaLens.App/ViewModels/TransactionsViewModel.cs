using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using System.Collections.ObjectModel;

namespace PesaLens.App.ViewModels;

// ── Grouped transactions (by date header) ────────────────────────────────────
public class TransactionGroup : ObservableCollection<Transaction>
{
    public string DateHeader { get; }

    public TransactionGroup(string dateHeader, IEnumerable<Transaction> items)
        : base(items)
    {
        DateHeader = dateHeader;
    }
}

public partial class TransactionsViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactions;

    // ── Filter state ─────────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _fromDate;
    [ObservableProperty] private DateTime _toDate;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private TransactionType? _selectedType = null;   // null = All
    [ObservableProperty] private string _dateRangeLabel = string.Empty;

    // ── UI state ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private bool _isEmpty;

    // ── Data ─────────────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<TransactionGroup> _groupedTransactions = [];

    // ── Type filter chips ─────────────────────────────────────────────────────
    public List<(string Label, TransactionType? Type)> TypeFilters { get; } =
    [
        ("All",       null),
        ("Sent",      TransactionType.SendMoney),
        ("Received",  TransactionType.ReceiveMoney),
        ("Paybill",   TransactionType.PayBill),
        ("Buy Goods", TransactionType.BuyGoods),
        ("Airtime",   TransactionType.AirtimePurchase),
        ("Withdraw",  TransactionType.Withdrawal),
    ];

    public TransactionsViewModel(ITransactionRepository transactions)
    {
        _transactions = transactions;
        ApplyThisWeekPreset();     // default range
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await ApplyFiltersAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        try { await ApplyFiltersAsync(); }
        finally { IsRefreshing = false; }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    public void SetTypeFilter(TransactionType? type)
    {
        SelectedType = type;
    }

    // Called from the bottom sheet "Save" button
    [RelayCommand]
    public async Task ApplyDateRangeAsync()
    {
        UpdateDateRangeLabel();
        await ApplyFiltersAsync();
    }

    // ── Date presets ──────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task ApplyTodayPresetAsync()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59);
        await ApplyDateRangeAsync();
    }

    [RelayCommand]
    public async Task ApplyThisWeekPresetAsync()
    {
        ApplyThisWeekPreset();
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    public async Task ApplyLastMonthPresetAsync()
    {
        var now = DateTime.Now;
        FromDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        ToDate = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
        await ApplyDateRangeAsync();
    }

    [RelayCommand]
    public async Task NavigateToDetailAsync(Transaction transaction)
    {
        var navParams = new Dictionary<string, object>
        {
            { "Transaction", transaction }
        };
        await Shell.Current.GoToAsync(nameof(Views.Transactions.TransactionDetailPage), navParams);
    }

    // ── Partial property callbacks (auto-reload on filter change) ─────────────

    partial void OnSearchQueryChanged(string value)
    {
        // Debouncing can be added here with a CancellationToken if needed;
        // for simplicity we reload immediately on change via command.
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedTypeChanged(TransactionType? value)
    {
        _ = ApplyFiltersAsync();
    }

    // ── Core filter logic ─────────────────────────────────────────────────────

    private async Task ApplyFiltersAsync()
    {
        List<Transaction> raw;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            raw = await _transactions.SearchAsync(SearchQuery.Trim());

            // Further filter by date and type in-memory
            raw = raw
                .Where(t => t.TransactionDate >= FromDate && t.TransactionDate <= ToDate)
                .ToList();
        }
        else
        {
            raw = await _transactions.GetFilteredAsync(
                from: FromDate,
                to: ToDate,
                categoryId: null,
                type: SelectedType);
        }

        // Apply type filter when search is active (GetFilteredAsync handles it otherwise)
        if (!string.IsNullOrWhiteSpace(SearchQuery) && SelectedType.HasValue)
            raw = raw.Where(t => t.Type == SelectedType!.Value).ToList();

        // Group by date
        var groups = raw
            .GroupBy(t => t.TransactionDate.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new TransactionGroup(FormatDateHeader(g.Key), g))
            .ToList();

        GroupedTransactions = new ObservableCollection<TransactionGroup>(groups);
        IsEmpty = GroupedTransactions.Count == 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyThisWeekPreset()
    {
        var today = DateTime.Today;
        int daysSinceMon = ((int)today.DayOfWeek + 6) % 7;
        FromDate = today.AddDays(-daysSinceMon);
        ToDate = FromDate.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
        UpdateDateRangeLabel();
    }

    private void UpdateDateRangeLabel()
    {
        DateRangeLabel = $"{FromDate:d MMM} – {ToDate:d MMM yyyy}";
    }

    private static string FormatDateHeader(DateTime date)
    {
        var today = DateTime.Today;
        if (date == today) return $"Today, {date:d MMM}";
        if (date == today.AddDays(-1)) return $"Yesterday, {date:d MMM}";
        return date.ToString("dddd, d MMM");
    }
}