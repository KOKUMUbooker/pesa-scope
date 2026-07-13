using Android.App;
using Android.Content.PM;
using PesaScope.App.Platforms.Android;

namespace PesaScope.App
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges =
            ConfigChanges.ScreenSize | ConfigChanges.Orientation |
            ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(
        [Android.Content.Intent.ActionSend, Android.Content.Intent.ActionSendto],
        Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
        DataSchemes = ["sms", "smsto", "mms", "mmsto"])]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnActivityResult(int requestCode, Result resultCode, global::Android.Content.Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == SmsRoleActivityResultHelper.RequestCode)
                SmsRoleActivityResultHelper.NotifyResult(resultCode == Result.Ok);
        }
    }
}
