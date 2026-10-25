using PesaScope.App.ViewModels;
using PesaScope.Core.Models;

namespace PesaScope.App.Views.Categories;

public partial class CategoriesPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly CategoriesViewModel _vm;
    private bool _loaded;

    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        // Repopulate rule picker whenever IsRefreshing goes false (refresh completed)
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CategoriesViewModel.IsRefreshing) && !_vm.IsRefreshing)
                PopulateRuleCategoryPicker();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        try
        {
            await _vm.LoadAsync();
        }
        finally
        {
            // Populate picker regardless, even if load partially failed
            PopulateRuleCategoryPicker();
        }
    }

    private async void OnHelpTapped(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PushModalAsync(new CategoriesHelpPage());
    }

    private async void OnCategoryItemTapped(object sender, EventArgs e)
    {
        if (sender is ImageButton btn && btn.BindingContext is CategorySpendRow row)
        {
            await Shell.Current.GoToAsync(
                $"//Transactions/TransactionsPage?categoryId={row.Category.Id}" +
                $"&fromDate={_vm.PeriodStart:yyyy-MM-dd}&toDate={_vm.PeriodEnd:yyyy-MM-dd}");
        }
    }

    // ── Rule category picker ──────────────────────────────────────────────────

    private void PopulateRuleCategoryPicker()
    {
        RuleCategoryPicker.ItemsSource = _vm.CategoryRows
            .Select(r => r.Category)
            .ToList();
    }

    private void OnRuleCategorySelected(object? sender, EventArgs e)
    {
        if (RuleCategoryPicker.SelectedItem is Category cat)
            _vm.RuleTargetCategory = cat;
    }
}