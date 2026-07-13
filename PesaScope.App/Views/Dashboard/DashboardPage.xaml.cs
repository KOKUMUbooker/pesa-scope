using PesaScope.App.ViewModels;

namespace PesaScope.App.Views.Dashboard;

public partial class DashboardPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly DashboardViewModel _vm;
    private bool _loaded;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        // Only load once — subsequent syncs via pull-to-refresh or the sync button
        if (!_vm.IsBusy && !_vm.RecentTransactions.Any())
            await _vm.LoadCommand.ExecuteAsync(null);
    }
}