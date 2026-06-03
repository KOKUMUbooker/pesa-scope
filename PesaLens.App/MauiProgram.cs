using Mopups.Hosting;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Data.Repositories;
using UraniumUI;

namespace PesaLens.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pesalens.db");

            builder.Services.AddSingleton(new DatabaseService(dbPath));
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

            return builder.Build();
        }
    }
}
