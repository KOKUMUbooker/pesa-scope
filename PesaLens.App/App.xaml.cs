using PesaLens.App.Data.Repositories;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Views.Onboarding;
using PesaLens.App.Views.Security;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace PesaLens.App;

public partial class App : Application
{
    private readonly IAppSettingsRepository _appSettingsRepo;
    private readonly ISecuritySettingsRepository _securitySettingsRepo;
    private readonly IServiceProvider _services;

    // Signals when DB init + seeding are done
    private readonly TaskCompletionSource _dbReady = new();

    public App(
        DatabaseService databaseService,
        DatabaseSeeder seeder,
        IAppSettingsRepository appSettingsRepo,
        ISecuritySettingsRepository securitySettingsRepo,
        IServiceProvider services)
    {
        InitializeComponent();

        _appSettingsRepo = appSettingsRepo;
        _securitySettingsRepo = securitySettingsRepo;
        _services = services;

        // Subscribe to notification tap event
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;

        // Kick off init — when done, signal _dbReady
        _ = InitializeAsync(databaseService, seeder);
        _services = services;
    }

    private async Task InitializeAsync(DatabaseService databaseService, DatabaseSeeder seeder)
    {
        try
        {
            await databaseService.InitializeAsync();
            await seeder.SeedAsync();
            _dbReady.TrySetResult();
        }
        catch (Exception ex)
        {
            _dbReady.TrySetException(ex);
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Return a blank window immediately so the splash screen can dismiss.
        // Once the DB is ready, swap the window's page to the correct start page.
        var window = new Window(new ContentPage()); // blank placeholder

        _ = SetStartPageAsync(window);

        return window;
    }

    private async Task SetStartPageAsync(Window window)
    {
        try
        {
            // Wait for DB init + seeding to finish before reading settings
            await _dbReady.Task;

            var settings = await _appSettingsRepo.GetAsync();
            var securitySettings = await _securitySettingsRepo.GetAsync();

            Page startPage;

            if (!settings.OnboardingComplete)
                startPage = _services.GetRequiredService<WelcomePage>(); 
            else if (securitySettings.BiometricsEnabled || securitySettings.PinHash is not null)
                startPage = _services.GetRequiredService<AppLockPage>(); 
            else
                startPage = new AppShell();

            // Switch to the real page on the UI thread
            window.Page = startPage;
        }
        catch (Exception ex)
        {
            // Surface the error visibly rather than hanging on a blank screen
            window.Page = new ContentPage
            {
                Content = new Label
                {
                    Text = $"Startup error:\n\n{ex.Message}",
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(24)
                }
            };
        }
    }

    static void OnNotificationActionTapped(NotificationActionEventArgs e)
    {
        if (!e.IsTapped) return;
        Shell.Current.GoToAsync("//Budgets/BudgetsPage");
    }
}

// <summary>
// App constructor
//  └── fires InitializeAsync(fire-and-forget, sets _dbReady when done)

// CreateWindow(called almost immediately by MAUI)
//  └── returns Window(blank page) instantly — splash screen dismisses
//  └── fires SetStartPageAsync

// SetStartPageAsync
//  └── awaits _dbReady.Task  ← waits here until InitializeAsync completes
//  └── reads settings
//  └── sets window.Page = WelcomePage / AppLockPage / AppShell
// </summary>