using Android.App;
using Android.Content;
using Android.OS;

namespace PesaScope.App;

// HeadlessSmsSendService.cs
[Service(
    Exported = true,
    Name = "com.bkokumu.pesascope.HeadlessSmsSendService",
    Permission = "android.permission.SEND_RESPOND_VIA_MESSAGE")]
[IntentFilter(
    ["android.intent.action.RESPOND_VIA_MESSAGE"],
    Categories = ["android.intent.category.DEFAULT"],
    DataSchemes = ["sms", "smsto", "mms", "mmsto"])]
public class HeadlessSmsSendService : Service
{
    public override IBinder? OnBind(Intent? intent) => null;
}