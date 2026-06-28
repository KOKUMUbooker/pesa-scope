using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.Core.Models;
using PesaLens.App.Views.Transactions;

namespace PesaLens.App.ViewModels;

[QueryProperty(nameof(InitialCategoryIdRaw), "categoryId")]
public partial class TransactionsViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;

    // Set by shell navigation, consumed once on first load, InitialCategoryIdRaw is received as string
    private int? _initialCategoryId;

    public string? InitialCategoryIdRaw
    {
        get => _initialCategoryId?.ToString();
        set
        {
            _initialCategoryId = int.TryParse(value, out var id) ? id : null;
            // Apply immediately so OnAppearing can read it before LoadAsync runs
            if (_initialCategoryId.HasValue)
            {
                SelectedCategoryId = _initialCategoryId;
                _initialCategoryId = null;
            }
        }
    }

    // ── Filter state ──────────────────────────────────────────────────────────

    [ObservableProperty] private DateTime _fromDate = GetWeekStart();
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private TransactionType? _selectedType = null;
    [ObservableProperty] private int? _selectedCategoryId = null;
    [ObservableProperty] private string _dateRangeLabel = "This Week";
    [ObservableProperty] private bool _isLoading = true;

    // ── Data ──────────────────────────────────────────────────────────────────

    [ObservableProperty] private List<TransactionGroup> _groupedTransactions = [];
    [ObservableProperty] private List<Category> _categories = [];
    [ObservableProperty] private bool _isEmpty;

    // ── Type filter chips (bound to CollectionView) ───────────────────────────

    public List<TypeFilterChip> TypeChips { get; } =
    [
        new("All",        null,                              true),
        new("Sent",       TransactionType.SendMoney),
        new("Received",   TransactionType.ReceiveMoney),
        new("Paybill",    TransactionType.PayBill),
        new("Buy Goods",  TransactionType.BuyGoods),
        new("Airtime",    TransactionType.AirtimePurchase),
        new("Withdrawal", TransactionType.Withdrawal),
    ];

    public TransactionsViewModel(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;

        Categories = await _categoryRepo.GetAllActiveAsync();

        List<Transaction> transactions;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            transactions = await _transactionRepo.SearchAsync(SearchQuery);
            // Apply date + type on top of search results
            var rangeEnd = ToDate.AddDays(1);
            transactions = transactions
                .Where(t => t.TransactionDate >= FromDate && t.TransactionDate <= rangeEnd)
                .ToList();
            if (SelectedType.HasValue)
                transactions = transactions.Where(t => t.Type == SelectedType).ToList();
            if (SelectedCategoryId.HasValue)
                transactions = transactions.Where(t => t.CategoryId == SelectedCategoryId).ToList();
        }
        else
        {
            transactions = await _transactionRepo.GetFilteredAsync(
                from: FromDate,
                to: ToDate.AddDays(1),
                categoryId: SelectedCategoryId,
                type: SelectedType);
        }

        GroupedTransactions = transactions
            .GroupBy(t => t.TransactionDate.ToLocalTime().Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new TransactionGroup(
                g.Key,
                g.OrderByDescending(t => t.TransactionDate).ToList(),
                Categories))
            .ToList();

        IsEmpty = GroupedTransactions.Count == 0;
        IsLoading = false;
    }

    // ── Search ────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task OnSearchChangedAsync() => await LoadAsync();

    // ── Type filter ───────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SelectTypeAsync(TypeFilterChip chip)
    {
        foreach (var c in TypeChips) c.IsSelected = false;
        chip.IsSelected = true;
        SelectedType = chip.Type;
        await LoadAsync();
    }

    // ── Category filter ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SelectCategoryAsync(Category? category)
    {
        SelectedCategoryId = category?.Id;
        await LoadAsync();
    }

    // ── Date range presets ────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SetPresetTodayAsync()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
        DateRangeLabel = "Today";
        await LoadAsync();
    }

    [RelayCommand]
    public async Task SetPresetWeekAsync()
    {
        FromDate = GetWeekStart();
        ToDate = DateTime.Today;
        DateRangeLabel = "This Week";
        await LoadAsync();
    }

    [RelayCommand]
    public async Task SetPresetThisMonthAsync()
    {
        FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ToDate = DateTime.Today;
        DateRangeLabel = DateTime.Today.ToString("MMMM yyyy");
        await LoadAsync();
    }

    [RelayCommand]
    public async Task SetPresetLastMonthAsync()
    {
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        FromDate = first;
        ToDate = first.AddMonths(1).AddDays(-1);
        DateRangeLabel = first.ToString("MMMM yyyy");
        await LoadAsync();
    }

    // ── Custom range (called by bottom sheet Save button) ─────────────────────

    [RelayCommand]
    public async Task ApplyCustomRangeAsync()
    {
        if (FromDate > ToDate) (FromDate, ToDate) = (ToDate, FromDate);
        DateRangeLabel = $"{FromDate:d MMM} – {ToDate:d MMM}";
        await LoadAsync();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task OpenTransactionAsync(Transaction transaction) =>
        await Shell.Current.GoToAsync(
            $"{nameof(TransactionDetailPage)}?code={transaction.MpesaCode}");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DateTime GetWeekStart()
    {
        var today = DateTime.Today;
        int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-diff);
    }
}

// ── Helper models ─────────────────────────────────────────────────────────────

public class TransactionGroup
{
    public string DateLabel { get; }
    public List<Transaction> Transactions { get; }
    private readonly List<Category> _categories;

    public TransactionGroup(DateTime date, List<Transaction> txs, List<Category> categories)
    {
        _categories = categories;
        Transactions = txs;
        var today = DateTime.Today;
        DateLabel = date == today ? "Today"
                     : date == today.AddDays(-1) ? "Yesterday"
                     : date.ToString("ddd, d MMM yyyy");
    }

    public string GetCategoryName(int categoryId) =>
        _categories.FirstOrDefault(c => c.Id == categoryId)?.Name ?? "Uncategorized";
}

public class TypeFilterChip : ObservableObject
{
    public string Label { get; }
    public TransactionType? Type { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public TypeFilterChip(string label, TransactionType? type, bool isSelected = false)
    {
        Label = label;
        Type = type;
        _isSelected = isSelected;
    }
}