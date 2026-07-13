using PesaScope.App.ViewModels;

namespace PesaScope.App.Views.Transactions;

public partial class TransactionDetailPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly TransactionDetailViewModel _vm;

    public TransactionDetailPage(TransactionDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }
}