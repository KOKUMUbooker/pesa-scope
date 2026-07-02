using PesaLens.App.Services.Interfaces;
using PesaLens.App.Views.Budgets;
using PesaLens.App.Views.Settings;
using PesaLens.App.Views.Transactions;

namespace PesaLens.App
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
            Routing.RegisterRoute("budgetHistory", typeof(BudgetHistoryPage));
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
