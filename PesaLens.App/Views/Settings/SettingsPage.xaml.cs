using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Settings;

public partial class SettingsPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly SettingsViewModel _vm;
    private bool _loaded;
    private bool _settingsReady; // guards against Toggled firing during bind

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        _settingsReady = false;
        await _vm.LoadAsync();
        _settingsReady = true;
        // SyncCurrencyButtons();
    }

    // ── Toggle handlers ───────────────────────────────────────────────────────
    // Switches in MAUI fire Toggled on initial bind

    private async void OnDarkModeToggled(object? sender, ToggledEventArgs e)
    {
        if (!_settingsReady) return;
        await _vm.ToggleDarkModeCommand.ExecuteAsync(e.Value);
    }

    private async void OnBudgetNotificationsToggled(object? sender, ToggledEventArgs e)
    {
        if (!_settingsReady) return;
        await _vm.ToggleBudgetNotificationsCommand.ExecuteAsync(e.Value);
    }

    private async void OnBiometricToggled(object? sender, ToggledEventArgs e)
    {
        if (!_settingsReady) return;
        await _vm.ToggleBiometricLockCommand.ExecuteAsync(e.Value);
    }

    private async void OnPinLockToggled(object? sender, ToggledEventArgs e)
    {
        if (!_settingsReady) return;
        await _vm.TogglePinLockCommand.ExecuteAsync(e.Value);
    }

    // ── Currency picker ───────────────────────────────────────────────────────

    //private async void OnKshSelected(object? sender, EventArgs e)
    //{
    //    await _vm.SetCurrencyCommand.ExecuteAsync("Ksh");
    //    SyncCurrencyButtons();
    //}

    //private async void OnKesSelected(object? sender, EventArgs e)
    //{
    //    await _vm.SetCurrencyCommand.ExecuteAsync("KES");
    //    SyncCurrencyButtons();
    //}

    // <summary>
    // Visually highlights the active currency button to match the VM state.
    // </summary>
    //private void SyncCurrencyButtons()
    //{
    //    bool kshActive = _vm.CurrencyDisplay == "Ksh";

    //    KshButton.BackgroundColor = kshActive
    //        ? (Color)Application.Current!.Resources["PrimaryContainer"]
    //        : Colors.Transparent;
    //    KshButton.TextColor = kshActive
    //        ? (Color)Application.Current!.Resources["Primary"]
    //        : (Color)Application.Current!.Resources["OnSurfaceVariant"];
    //    KshButton.FontAttributes = kshActive ? FontAttributes.Bold : FontAttributes.None;

    //    KesButton.BackgroundColor = !kshActive
    //        ? (Color)Application.Current!.Resources["PrimaryContainer"]
    //        : Colors.Transparent;
    //    KesButton.TextColor = !kshActive
    //        ? (Color)Application.Current!.Resources["Primary"]
    //        : (Color)Application.Current!.Resources["OnSurfaceVariant"];
    //    KesButton.FontAttributes = !kshActive ? FontAttributes.Bold : FontAttributes.None;
    //}
}