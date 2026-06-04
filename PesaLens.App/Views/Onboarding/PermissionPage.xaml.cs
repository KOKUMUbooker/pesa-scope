namespace PesaLens.App.Views.Onboarding;

public partial class PermissionPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly IServiceProvider _services;

    public PermissionPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    private async void OnGrantClicked(object? sender, EventArgs e)
    {
        GrantButton.IsEnabled = false;
        DeniedBanner.IsVisible = false;

        var status = await RequestSmsPermissionAsync();

        if (status == PermissionStatus.Granted)
        {
            // Permission granted — move to import progress
            if (Application.Current?.Windows.FirstOrDefault() is Window window)
                window.Page = _services.GetRequiredService<ImportProgressPage>();
        }
        else
        {
            // Denied — show the guidance banner and re-enable the button
            DeniedBanner.IsVisible = true;
            GrantButton.IsEnabled = true;
            GrantButton.Text = "Try Again";
        }
    }

    private static async Task<PermissionStatus> RequestSmsPermissionAsync()
    {
        // Check current status first — if already granted, skip the dialog
        var status = await Permissions.CheckStatusAsync<Permissions.Sms>();

        if (status == PermissionStatus.Granted)
            return status;

        // Request it — shows the Android system dialog
        return await Permissions.RequestAsync<Permissions.Sms>();
    }
}