using PesaLens.App.Data.Repositories;

namespace PesaLens.App
{
    public partial class App : Application
    {
        public App(DatabaseService databaseService, DatabaseSeeder seeder)
        {
            InitializeComponent();

            MainPage = new AppShell();

            _ = InitializeAsync(databaseService, seeder);
        }

        private async Task InitializeAsync(DatabaseService databaseService, DatabaseSeeder seeder)
        {
            await databaseService.InitializeAsync();

            await seeder.SeedAsync();
        }
    }
}
