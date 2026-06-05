using LiveChartsCore.SkiaSharpView.Maui;
using Mopups.Hosting;
using PesaLens.App.Data.Repositories;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Services;
using PesaLens.App.Services.Interfaces;
using PesaLens.App.ViewModels;
using PesaLens.App.Views.Onboarding;
using PesaLens.App.Views.Security;
using SkiaSharp.Views.Maui.Controls.Hosting;
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
                .UseSkiaSharp()
                .UseLiveCharts()
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

            // Onboarding pages — transient because they are created once and discarded
            builder.Services.AddTransient<WelcomePage>();
            builder.Services.AddTransient<PermissionPage>();
            builder.Services.AddTransient<ImportProgressPage>();
            builder.Services.AddTransient<AppLockPage>();

            // View models
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // DatabaseService registered as singleton — App.cs resolves and inits it
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
            builder.Services.AddSingleton<ISmsReaderService,SmsReaderService>();
            builder.Services.AddSingleton<IMpesaSmsParser,MpesaSmsParser>();
            builder.Services.AddSingleton<DatabaseSeeder>();

            return builder.Build();
        }
    }
}
