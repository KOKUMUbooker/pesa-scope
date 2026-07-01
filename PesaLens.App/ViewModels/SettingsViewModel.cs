using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Services.Interfaces;
using PesaLens.Core.Models;
using PesaLens.App.Data;

namespace PesaLens.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsRepository _appSettingsRepo;
    private readonly ISyncMetadataRepository _syncMetadataRepo;
    private readonly DatabaseSeeder _seeder;
    private readonly DatabaseService _databaseService;
    private readonly IBiometricAuthService _biometricAuthService;

    private AppSettings _appSettings = new();

    // ── General ───────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _budgetNotificationsEnabled;
    [ObservableProperty] private string _currencyDisplay = "Ksh";
    [ObservableProperty] private string _lastSyncedText = "Never";

    // ── Security ──────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _appLockEnabled;

    // ── About ─────────────────────────────────────────────────────────────────

    public string AppVersion =>
        AppInfo.VersionString is { } v ? $"v{v}" : "v1.0.0";

    public SettingsViewModel(
        IAppSettingsRepository appSettingsRepo,
        ISyncMetadataRepository syncMetadataRepo,
        DatabaseSeeder seeder,
        IBiometricAuthService biometricAuthService,
        DatabaseService databaseService)
    {
        _appSettingsRepo = appSettingsRepo;
        _syncMetadataRepo = syncMetadataRepo;
        _seeder = seeder;
        _biometricAuthService = biometricAuthService;
        _databaseService = databaseService;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        _appSettings = await _appSettingsRepo.GetAsync();
        var syncMeta = await _syncMetadataRepo.GetAsync();

        // Populate observable properties without triggering saves
        IsDarkMode = _appSettings.Theme == PesaLens.Core.Models.AppTheme.Dark;
        BudgetNotificationsEnabled = _appSettings.BudgetNotificationsEnabled;
        AppLockEnabled = _appSettings.AppLockEnabled;

        CurrencyDisplay = _appSettings.CurrencyDisplay == PesaLens.Core.Models.CurrencyDisplay.Ksh ? "Ksh" : "KES";

        LastSyncedText = syncMeta.LastSyncTime == DateTime.MinValue
            ? "Never"
            : FormatSyncTime(syncMeta.LastSyncTime);
    }

    // ── Toggle handlers ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task ToggleDarkModeAsync(bool value)
    {
        IsDarkMode = value;
        _appSettings.Theme = value ? PesaLens.Core.Models.AppTheme.Dark : PesaLens.Core.Models.AppTheme.Light;

        Application.Current!.UserAppTheme = value
            ? Microsoft.Maui.ApplicationModel.AppTheme.Dark
            : Microsoft.Maui.ApplicationModel.AppTheme.Light;

        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    [RelayCommand]
    public async Task ToggleBudgetNotificationsAsync(bool value)
    {
        if (value)
        {
            // Request permission when user tries to enable notifications
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();

            if (status != PermissionStatus.Granted)
            {
                // Revert the toggle — permission was denied
                BudgetNotificationsEnabled = false;
                await Shell.Current.DisplayAlertAsync(
                    "Permission Required",
                    "Notification permission was denied. Enable it in your device settings to receive budget alerts.",
                    "OK");
                return;
            }
        }

        BudgetNotificationsEnabled = value;
        _appSettings.BudgetNotificationsEnabled = value;
        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    [RelayCommand]
    public async Task SetCurrencyAsync(string value)
    {
        CurrencyDisplay = value;
        _appSettings.CurrencyDisplay = value == "Ksh"
            ? PesaLens.Core.Models.CurrencyDisplay.Ksh
            : PesaLens.Core.Models.CurrencyDisplay.KES;
        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    [RelayCommand]
    public async Task ToggleAppLockAsync(bool value)
    {
        if (value)
        {
            var available = await _biometricAuthService.IsAvailableAsync();
            if (!available)
            {
                AppLockEnabled = false; // revert
                await Shell.Current.DisplayAlertAsync(
                    "Unavailable",
                    "Set up a screen lock (PIN, pattern, fingerprint, or face) on your device first.",
                    "OK");
                return;
            }
        }

        AppLockEnabled = value;
        _appSettings.AppLockEnabled = value;
        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    // ── Data management ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SyncNowAsync()
    {
        // Trigger SMS sync service — placeholder until SyncService is implemented
        await Shell.Current.DisplayAlertAsync("Sync", "Sync will be available soon.", "OK");
        LastSyncedText = FormatSyncTime(DateTime.UtcNow);
    }

    [RelayCommand]
    public async Task ClearAllDataAsync()
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Clear All Data",
            "This will permanently delete all your transactions, budgets, and categories. This cannot be undone.",
            "Delete Everything",
            "Cancel");

        if (!confirmed) return;

        await _seeder.ReseedAsync(_databaseService);

        // Restart to onboarding
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = new AppShell();
    }

    // ── About ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SendFeedbackAsync()
    {
        const string email = "feedback@pesalens.app";
        const string subject = "PesaLens Feedback";

        if (Email.Default.IsComposeSupported)
            await Email.Default.ComposeAsync(subject, string.Empty, email);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatSyncTime(DateTime utc)
    {
        var local = utc.ToLocalTime();
        var diff = DateTime.Now - local;

        return diff.TotalMinutes < 1 ? "Just now"
             : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes}m ago"
             : diff.TotalHours < 24 ? $"{(int)diff.TotalHours}h ago"
             : local.ToString("MMM d");
    }
}