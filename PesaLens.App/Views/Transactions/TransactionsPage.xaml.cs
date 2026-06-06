using PesaLens.Core.Models;
using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Transactions;

public partial class TransactionsPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly TransactionsViewModel _vm;
    private CancellationTokenSource?       _searchDebounce;
    private int?                           _activeCategoryId;

    public TransactionsPage(TransactionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        BuildCategoryChips();
    }

    // ── Category chips (3.6) ──────────────────────────────────────────────────

    private void BuildCategoryChips()
    {
        CategoryChipsLayout.Children.Clear();

        // "All" chip
        CategoryChipsLayout.Children.Add(MakeCategoryChip("All", null));

        foreach (var cat in _vm.Categories)
            CategoryChipsLayout.Children.Add(MakeCategoryChip(cat.Name, cat));
    }

    private Border MakeCategoryChip(string label, Category? category)
    {
        bool isActive = category == null
            ? _activeCategoryId == null
            : _activeCategoryId == category.Id;

        var chip = new Border
        {
            Padding = new Thickness(12, 8),
            StrokeThickness = 0,
            BackgroundColor = isActive
                ? (Color)Application.Current!.Resources["Primary"]
                : (Color)Application.Current!.Resources["SurfaceVariant"],
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Margin = new Thickness(0, 0, 6, 0)
        };

        chip.Content = new Label
        {
            Text = label,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            TextColor = isActive
                ? (Color)Application.Current!.Resources["OnPrimary"]
                : (Color)Application.Current!.Resources["OnSurfaceVariant"]
        };

        chip.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                _activeCategoryId = category?.Id;
                await _vm.SelectCategoryCommand.ExecuteAsync(category);
                BuildCategoryChips(); // refresh chip active states
            })
        });

        return chip;
    }

    // ── Search with 400ms debounce ────────────────────────────────────────────

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();

        var token = _searchDebounce.Token;
        Task.Delay(400, token).ContinueWith(async t =>
        {
            if (t.IsCanceled) return;
            await MainThread.InvokeOnMainThreadAsync(() => _vm.SearchChangedCommand.Execute(null));
        });
    }

    private void OnClearSearch(object? sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        _vm.SearchQuery  = string.Empty;
        _ = _vm.LoadAsync();
    }

    // ── Date range bottom sheet ───────────────────────────────────────────────

    private void OnDateFilterTapped(object? sender, EventArgs e) =>
        DateRangeSheet.IsPresented = true;

    private async void OnPresetToday(object? sender, EventArgs e)
    {
        await _vm.SetPresetTodayCommand.ExecuteAsync(null);
        DateRangeSheet.IsPresented = false;
    }

    private async void OnPresetWeek(object? sender, EventArgs e)
    {
        await _vm.SetPresetWeekCommand.ExecuteAsync(null);
        DateRangeSheet.IsPresented = false;
    }

    private async void OnPresetThisMonth(object? sender, EventArgs e)
    {
        await _vm.SetPresetThisMonthCommand.ExecuteAsync(null);
        DateRangeSheet.IsPresented = false;
    }

    private async void OnPresetLastMonth(object? sender, EventArgs e)
    {
        await _vm.SetPresetLastMonthCommand.ExecuteAsync(null);
        DateRangeSheet.IsPresented = false;
    }

    private async void OnApplyCustomRange(object? sender, EventArgs e)
    {
        await _vm.ApplyCustomRangeCommand.ExecuteAsync(null);
        DateRangeSheet.IsPresented = false;
    }
}