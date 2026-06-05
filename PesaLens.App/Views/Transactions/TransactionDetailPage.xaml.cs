using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Transactions;

public partial class TransactionDetailPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly TransactionDetailViewModel _vm;

    public TransactionDetailPage(TransactionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}