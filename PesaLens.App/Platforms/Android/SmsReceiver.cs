using Android.App;
using Android.Content;

namespace PesaLens.App;

/// <summary>
/// Required stub for Default SMS App role eligibility.
/// Real-time capture is handled by MpesaSmsReceiver (SMS_RECEIVED),
/// which fires regardless of default app status.
/// </summary>
[BroadcastReceiver(Exported = true, Permission = "android.permission.BROADCAST_SMS")]
[IntentFilter(["android.provider.Telephony.SMS_DELIVER"])]
public class SmsReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        // SMS_DELIVER only fires when PesaLens is the default app (onboarding only).
        // MpesaSmsReceiver handles all post-onboarding capture via SMS_RECEIVED.
        // No action needed here.
    }
}