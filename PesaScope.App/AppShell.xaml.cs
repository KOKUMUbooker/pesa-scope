using PesaScope.App.Services.Interfaces;
using PesaScope.App.Views.Budgets;
using PesaScope.App.Views.Settings;
using PesaScope.App.Views.Transactions;

namespace PesaScope.App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for pages that are NOT in the tab bar.
            // These are pushed on top of a tab's navigation stack via
            // Shell.Current.GoToAsync("TransactionDetailPage?code=RG84XY1234")
            Routing.RegisterRoute(nameof(TransactionDetailPage), typeof(TransactionDetailPage));
            Routing.RegisterRoute(nameof(BudgetHistoryPage), typeof(BudgetHistoryPage));
            Routing.RegisterRoute(nameof(ExportPage), typeof(ExportPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var snapshotService = ServiceLocator.GetService<IBudgetSnapshotService>();
            if (snapshotService is not null)
                await snapshotService.SnapshotPreviousMonthIfNeededAsync();
        }
    }
}
