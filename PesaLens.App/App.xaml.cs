using PesaLens.App.Data.Repositories;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Views.Onboarding;
using PesaLens.App.Views.Security;

namespace PesaLens.App;

public partial class App : Application
{
    private readonly IAppSettingsRepository _appSettingsRepo;
    private readonly ISecuritySettingsRepository _securitySettingsRepo;

    // Signals when DB init + seeding are done
    private readonly TaskCompletionSource _dbReady = new();

    public App(
        DatabaseService databaseService,
        DatabaseSeeder seeder,
        IAppSettingsRepository appSettingsRepo,
        ISecuritySettingsRepository securitySettingsRepo)
    {
        InitializeComponent();

        _appSettingsRepo = appSettingsRepo;
        _securitySettingsRepo = securitySettingsRepo;

        // Kick off init — when done, signal _dbReady
        _ = InitializeAsync(databaseService, seeder);
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
                startPage = new WelcomePage();
            else if (securitySettings.BiometricsEnabled || securitySettings.PinHash is not null)
                startPage = new AppLockPage();
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