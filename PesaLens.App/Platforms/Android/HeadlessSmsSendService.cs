using Android.App;
using Android.Content;
using Android.OS;

namespace PesaLens.App;

// HeadlessSmsSendService.cs
[Service(
    Exported = true,
    Name = "com.bkokumu.pesalens.HeadlessSmsSendService",
    Permission = "android.permission.SEND_RESPOND_VIA_MESSAGE")]
[IntentFilter(
    ["android.intent.action.RESPOND_VIA_MESSAGE"],
    Categories = ["android.intent.category.DEFAULT"],
    DataSchemes = ["sms", "smsto", "mms", "mmsto"])]
public class HeadlessSmsSendService : Service
{
    public override IBinder? OnBind(Intent? intent) => null;
}