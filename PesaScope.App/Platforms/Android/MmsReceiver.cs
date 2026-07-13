using Android.App;
using Android.Content;

namespace PesaScope.App;

// MmsReceiver.cs
[BroadcastReceiver(Exported = true, Name = "com.bkokumu.pesascope.MmsReceiver", Permission = "android.permission.BROADCAST_WAP_PUSH")]
[IntentFilter(["android.provider.Telephony.WAP_PUSH_DELIVER"], DataMimeType = "application/vnd.wap.mms-message")]
public class MmsReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) { }
}