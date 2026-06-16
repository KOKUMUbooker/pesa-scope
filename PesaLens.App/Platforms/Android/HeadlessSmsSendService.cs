using Android.App;
using Android.Content;
using Android.OS;

namespace PesaLens.App;

[Service(Exported = true, Permission = "android.permission.SEND_RESPOND_VIA_MESSAGE")]
[IntentFilter(["android.intent.action.RESPOND_VIA_MESSAGE"], Categories = ["android.intent.category.DEFAULT"], DataSchemes = ["sms", "smsto", "mms", "mmsto"])]
public class HeadlessSmsSendService : Service
{
    public override IBinder? OnBind(Intent? intent) => null;
}