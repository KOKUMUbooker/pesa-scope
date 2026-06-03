using InputKit.Shared.Controls;
using Mopups.Hosting;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Repositories;
using UraniumUI;

namespace PesaLens.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pesalens.db");
            var dbService = new DatabaseService(dbPath);
            var connection = await dbService.InitializeAsync();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureMopups()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .UseUraniumUIBlurs()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                    fonts.AddMaterialSymbolsFonts();
                });
            builder.Services.AddMopupsDialogs();
            builder.Services.AddSingleton(connection);
            builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
            builder.Services.AddSingleton<IAutoCategorizationRuleRepository, AutoCategorizationRuleRepository>();
            builder.Services.AddSingleton<IBudgetRepository, BudgetRepository>();
            builder.Services.AddSingleton<IOverallBudgetRepository, OverallBudgetRepository>();
            builder.Services.AddSingleton<ISyncMetadataRepository, SyncMetadataRepository>();
            builder.Services.AddSingleton<IAppSettingsRepository, AppSettingsRepository>();
            builder.Services.AddSingleton<ISecuritySettingsRepository, SecuritySettingsRepository>();
            builder.Services.AddSingleton<IExportHistoryRepository, ExportHistoryRepository>();
            builder.Services.AddSingleton<DatabaseSeeder>();

            var app = builder.Build();
            var seeder = app.Services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();

            return app;
        }
    }
}
