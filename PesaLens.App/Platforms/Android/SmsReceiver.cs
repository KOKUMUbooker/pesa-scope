using Android.App;
using Android.Content;
using Android.Provider;

namespace PesaLens.App;

/// <summary>
/// Required stub for Default SMS App role eligibility.
/// Real-time capture is handled by MpesaSmsReceiver (SMS_RECEIVED),
/// which fires regardless of default app status.
/// </summary>
[BroadcastReceiver(Exported = true, Name = "com.bkokumu.pesalens.SmsReceiver", Permission = "android.permission.BROADCAST_SMS")]
[IntentFilter(["android.provider.Telephony.SMS_DELIVER"])]
public class SmsReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        // Forward to the user's real SMS app so messages aren't lost
        // while PesaLens is temporarily the default during onboarding.
        var messages = Telephony.Sms.Intents.GetMessagesFromIntent(intent);
        if (messages is null) return;

        foreach (var msg in messages)
        {
            if (msg?.OriginatingAddress?.Contains("MPESA",
                StringComparison.OrdinalIgnoreCase) == true)
                continue; // we handle these ourselves via SMS_RECEIVED

            // Non-M-Pesa message — write it to the SMS inbox so the
            // user's real messaging app can display it
            var values = new Android.Content.ContentValues();
            values.Put("address", msg?.OriginatingAddress);
            values.Put("body", msg?.MessageBody);
            if (msg?.TimestampMillis != null) values.Put("date", msg.TimestampMillis);
            values.Put("read", 0);

            context?.ContentResolver?.Insert(
                Android.Net.Uri.Parse("content://sms/inbox")!, values);
        }
    }
}