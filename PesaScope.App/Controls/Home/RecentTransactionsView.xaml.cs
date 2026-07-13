using System.Windows.Input;
using PesaLens.Core.Models;

namespace PesaLens.App.Controls.Home;

public partial class RecentTransactionsView : ContentView
{
    public static readonly BindableProperty TransactionsProperty =
        BindableProperty.Create(
            nameof(Transactions),
            typeof(IList<Transaction>),
            typeof(RecentTransactionsView),
            defaultValue: new List<Transaction>());

    public static readonly BindableProperty ViewAllCommandProperty =
        BindableProperty.Create(
            nameof(ViewAllCommand),
            typeof(ICommand),
            typeof(RecentTransactionsView),
            defaultValue: null);

    public static readonly BindableProperty OpenTransactionCommandProperty =
        BindableProperty.Create(
            nameof(OpenTransactionCommand),
            typeof(ICommand),
            typeof(RecentTransactionsView),
            defaultValue: null);

    public IList<Transaction> Transactions
    {
        get => (IList<Transaction>)GetValue(TransactionsProperty);
        set => SetValue(TransactionsProperty, value);
    }

    public ICommand? ViewAllCommand
    {
        get => (ICommand?)GetValue(ViewAllCommandProperty);
        set => SetValue(ViewAllCommandProperty, value);
    }

    public ICommand? OpenTransactionCommand
    {
        get => (ICommand?)GetValue(OpenTransactionCommandProperty);
        set => SetValue(OpenTransactionCommandProperty, value);
    }

    public RecentTransactionsView()
    {
        InitializeComponent();
    }
}