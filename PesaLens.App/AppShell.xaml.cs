using PesaLens.App.Views.Categories;
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
            Routing.RegisterRoute(nameof(EditCategoryPage), typeof(EditCategoryPage));
        }
    }
}
