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
        SyncThemeButtons();
        // SyncCurrencyButtons();
    }

    // ── Toggle handlers ───────────────────────────────────────────────────────
    // Switches in MAUI fire Toggled on initial bind
    private async void OnBudgetNotificationsToggled(object? sender, ToggledEventArgs e)
    {
        if (!_settingsReady) return;
        await _vm.ToggleBudgetNotificationsCommand.ExecuteAsync(e.Value);
    }

    private async void OnAppLockToggled(object? sender, ToggledEventArgs e)
    {
        if (!_settingsReady) return;
        await _vm.ToggleAppLockCommand.ExecuteAsync(e.Value);
    }

    private async void OnSystemThemeSelected(object? sender, EventArgs e)
    {
        await _vm.SetThemeCommand.ExecuteAsync(PesaLens.Core.Models.AppTheme.System);
        SyncThemeButtons();
    }

    private async void OnLightThemeSelected(object? sender, EventArgs e)
    {
        await _vm.SetThemeCommand.ExecuteAsync(PesaLens.Core.Models.AppTheme.Light);
        SyncThemeButtons();
    }

    private async void OnDarkThemeSelected(object? sender, EventArgs e)
    {
        await _vm.SetThemeCommand.ExecuteAsync(PesaLens.Core.Models.AppTheme.Dark);
        SyncThemeButtons();
    }

    private async void OnTransactionNotificationsToggled(object? sender, ToggledEventArgs e)
    {
        if (!_settingsReady) return;
        await _vm.ToggleTransactionNotificationsCommand.ExecuteAsync(e.Value);
    }

    private void SyncThemeButtons()
    {
        var active = _vm.CurrentTheme;
        var primary = (Color)Application.Current!.Resources["Primary"];
        var primaryContainer = (Color)Application.Current!.Resources["PrimaryContainer"];
        var onSurfaceVariant = (Color)Application.Current!.Resources["OnSurfaceVariant"];

        void Apply(Border border, Label label, bool isActive)
        {
            border.BackgroundColor = isActive ? primaryContainer : Colors.Transparent;
            border.Stroke = isActive ? primary : onSurfaceVariant;
            label.TextColor = isActive ? primary : onSurfaceVariant;
            label.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
        }

        Apply(SystemThemeButton, SystemThemeLabel, active == PesaLens.Core.Models.AppTheme.System);
        Apply(LightThemeButton, LightThemeLabel, active == PesaLens.Core.Models.AppTheme.Light);
        Apply(DarkThemeButton, DarkThemeLabel, active == PesaLens.Core.Models.AppTheme.Dark);
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