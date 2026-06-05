using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Transactions;

public partial class TransactionsPage : UraniumUI.Pages.UraniumContentPage
{
    public TransactionsPage(TransactionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
