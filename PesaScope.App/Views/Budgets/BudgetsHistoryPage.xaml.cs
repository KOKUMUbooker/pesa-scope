using PesaScope.App.ViewModels;

namespace PesaScope.App.Views.Budgets;

public partial class BudgetHistoryPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly BudgetHistoryViewModel _vm;

    public BudgetHistoryPage(BudgetHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Always reload — the user may have navigated back after a new month
        // rolled over and a snapshot was taken
        await _vm.LoadAsync();
    }
}