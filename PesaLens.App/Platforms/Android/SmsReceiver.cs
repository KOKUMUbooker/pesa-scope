using Android.App;
using Android.Content;

namespace PesaLens.App;

[BroadcastReceiver(Exported = true, Permission = "android.permission.BROADCAST_SMS")]
[IntentFilter(["android.provider.Telephony.SMS_DELIVER"])]
public class SmsReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        // Real-time capture implementation goes here later
    }
}