using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Transactions;

public partial class TransactionsPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly TransactionsViewModel _vm;

    public TransactionsPage(TransactionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reload on every appearance so edits/creates made on EditCategoryPage
        // are reflected immediately when the user navigates back
        await _vm.LoadAsync();
    }
}
