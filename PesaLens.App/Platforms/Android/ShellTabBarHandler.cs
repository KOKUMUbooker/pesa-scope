using Android.Content.Res;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace PesaLens.App.Platforms.Android;

public class PesaLensShellHandler : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
    {
        return new PesaLensBottomNavTracker();
    }
}

public class PesaLensBottomNavTracker : IShellBottomNavViewAppearanceTracker
{
    public void ResetAppearance(BottomNavigationView bottomView)
    {
        ApplyColors(bottomView);
    }

    public void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        ApplyColors(bottomView);
    }

    public void Dispose() { }

    private static void ApplyColors(BottomNavigationView bottomView)
    {
        // Resolve dark mode
        var uiMode = bottomView.Context?.Resources?.Configuration?.UiMode & UiMode.NightMask;
        bool isDark = uiMode == UiMode.NightYes;

        var bgColor = isDark
            ? global::Android.Graphics.Color.ParseColor("#111F19")
            : global::Android.Graphics.Color.ParseColor("#F7FAF8");

        bottomView.SetBackgroundColor(bgColor);
    }
}