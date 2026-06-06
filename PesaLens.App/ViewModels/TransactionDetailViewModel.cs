using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.Core.Models;

namespace PesaLens.App.ViewModels;

[QueryProperty(nameof(MpesaCode), "code")]
public partial class TransactionDetailViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;

    // ── State ─────────────────────────────────────────────────────────────────

    [ObservableProperty] private string _mpesaCode = string.Empty;
    [ObservableProperty] private Transaction? _transaction;
    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private string _categoryIcon = string.Empty;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isEditing = false;
    [ObservableProperty] private string _formattedAmount = string.Empty;
    [ObservableProperty] private Color _amountColor = Colors.Red;
    [ObservableProperty] private string _typeIcon = "💳";
    [ObservableProperty] private List<Category> _categories = [];

    public TransactionDetailViewModel(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    partial void OnMpesaCodeChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(MpesaCode)) return;

        IsLoading = true;

        // Fetch by MpesaCode — uses SearchAsync since the PK is an int
        var matches = await _transactionRepo.SearchAsync(MpesaCode);
        Transaction = matches.FirstOrDefault(t => t.MpesaCode == MpesaCode);

        if (Transaction is null)
        {
            IsLoading = false;
            return;
        }

        Note = Transaction.Note ?? string.Empty;

        var category = await _categoryRepo.GetByIdAsync(Transaction.CategoryId);
        CategoryName = category?.Name ?? "Uncategorized";
        CategoryIcon = category?.Icon ?? "tag";

        FormattedAmount = FormatAmount(Transaction);
        AmountColor = IsOutgoing(Transaction.Type) ? Color.FromArgb("#C0392B") : Color.FromArgb("#1A8C62");
        TypeIcon = GetTypeIcon(Transaction.Type);

        Categories = await _categoryRepo.GetAllActiveAsync();

        IsLoading = false;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public void ToggleEdit() => IsEditing = !IsEditing;

    [RelayCommand]
    public async Task SaveNoteAsync()
    {
        if (Transaction is null) return;

        await _transactionRepo.UpdateNoteAsync(Transaction.MpesaCode, Note);
        IsEditing = false;

        await Shell.Current.DisplayAlertAsync("Saved", "Note updated successfully.", "OK");
    }

    [RelayCommand]
    public async Task ChangeCategoryAsync(Category category)
    {
        if (Transaction is null) return;

        await _transactionRepo.UpdateCategoryAsync(Transaction.MpesaCode, category.Id);
        CategoryName = category.Name;
        CategoryIcon = category.Icon;

        await Shell.Current.DisplayAlertAsync("Updated", $"Category changed to {category.Name}.", "OK");
    }

    [RelayCommand]
    public async Task CopySmsAsync()
    {
        if (Transaction is null) return;
        await Clipboard.Default.SetTextAsync(Transaction.OriginalSms);
        await Shell.Current.DisplayAlertAsync("Copied", "SMS text copied to clipboard.", "OK");
    }

    [RelayCommand]
    public async Task ShareAsync()
    {
        if (Transaction is null) return;

        var text = $"PesaLens Transaction\n" +
                   $"Code: {Transaction.MpesaCode}\n" +
                   $"Amount: {FormattedAmount}\n" +
                   $"To/From: {Transaction.CounterpartyName}\n" +
                   $"Date: {Transaction.TransactionDate.ToLocalTime():MMM d yyyy, h:mm tt}\n" +
                   $"Category: {CategoryName}";

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = text,
            Title = "Share Transaction"
        });
    }

    [RelayCommand]
    public async Task GoBackAsync() =>
        await Shell.Current.GoToAsync("..");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsOutgoing(TransactionType type) => type is
        TransactionType.SendMoney or
        TransactionType.PayBill or
        TransactionType.BuyGoods or
        TransactionType.AirtimePurchase or
        TransactionType.Withdrawal or
        TransactionType.Fuliza or
        TransactionType.MShwari;

    private static string FormatAmount(Transaction tx)
    {
        var sign = IsOutgoing(tx.Type) ? "-" : "+";
        return $"{sign}Ksh {tx.Amount:N0}";
    }

    private static string GetTypeIcon(TransactionType type) => type switch
    {
        TransactionType.SendMoney => "📤",
        TransactionType.ReceiveMoney => "📥",
        TransactionType.PayBill => "🧾",
        TransactionType.BuyGoods => "🛒",
        TransactionType.AirtimePurchase => "📱",
        TransactionType.Withdrawal => "🏧",
        TransactionType.Deposit => "💰",
        TransactionType.Fuliza => "⚡",
        TransactionType.MShwari => "🏦",
        TransactionType.Reversal => "↩️",
        _ => "💳"
    };
}