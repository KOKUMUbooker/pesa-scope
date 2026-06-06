using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Budgets;

public partial class BudgetsPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly BudgetsViewModel _vm;

    public BudgetsPage(BudgetsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
