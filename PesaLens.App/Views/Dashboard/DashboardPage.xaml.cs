using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Dashboard;

public partial class DashboardPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Only load once — subsequent syncs via pull-to-refresh or the sync button
        if (!_vm.IsBusy && !_vm.RecentTransactions.Any())
            await _vm.LoadCommand.ExecuteAsync(null);
    }
}