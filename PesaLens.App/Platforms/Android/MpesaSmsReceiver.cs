using Android.App;
using Android.Content;
using Android.Provider;
using Android.Telephony;
using PesaLens.App.Data.Repositories;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Services.Interfaces;
using PesaLens.Core.Services.Interfaces;

namespace PesaLens.App;

/// <summary>
/// Listens for incoming SMS messages at all times (not just when default app).
/// Filters for MPESA sender, parses, and saves to the local DB.
/// </summary>
[BroadcastReceiver(Exported = true, Name = "com.bkokumu.pesalens.MpesaSmsReceiver")]
[IntentFilter(["android.provider.Telephony.SMS_RECEIVED"], Priority = 999)]
public class MpesaSmsReceiver : BroadcastReceiver
{
    private const string MpesaSender = "MPESA";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;
        if (intent.Action != Telephony.Sms.Intents.SmsReceivedAction) return;

        // Extract PDUs from the intent bundle
        var messages = Telephony.Sms.Intents.GetMessagesFromIntent(intent);
        if (messages is null || messages.Length == 0) return;

        // Group PDUs by originating address — multi-part SMS arrives as
        // multiple SmsMessage objects with the same sender
        var grouped = messages
            .Where(m => m?.OriginatingAddress != null)
            .GroupBy(m => m!.OriginatingAddress!.ToUpperInvariant());

        foreach (var group in grouped)
        {
            // Only process MPESA messages
            if (!group.Key.Contains(MpesaSender, StringComparison.OrdinalIgnoreCase))
                continue;

            // Reconstruct full body from all PDU parts
            var fullBody = string.Concat(group.Select(m => m!.MessageBody));
            var timestampMs = group.First()!.TimestampMillis;
            var smsId = ResolveInboxSmsId(timestampMs);

            // Hand off to background processing — OnReceive must return quickly
            _ = ProcessMpesaSmsAsync(context, fullBody, smsId, timestampMs);
        }
    }

    // ── Background processing ─────────────────────────────────────────────────

    private static async Task ProcessMpesaSmsAsync(
        Context context,
        string body,
        long smsId,
        long timestampMs)
    {
        try
        {
            // Resolve services manually — no DI container in BroadcastReceiver
            var parser = ServiceLocator.GetService<IMpesaSmsParser>();
            var transactionRepo = ServiceLocator.GetService<ITransactionRepository>();
            var syncMetaRepo = ServiceLocator.GetService<ISyncMetadataRepository>();
            var categorizationService = ServiceLocator.GetService<IAutoCategorizationService>();
            var appSettingsRepo = ServiceLocator.GetService<IAppSettingsRepository>();

            if (parser is null || transactionRepo is null) return;

            // Only process if onboarding is complete
            var settings = await appSettingsRepo!.GetAsync();
            if (!settings.OnboardingComplete) return;

            var transaction = parser.Parse(body, smsId, timestampMs);
            if (transaction is null) return;

            // Deduplicate: skip if this SmsId is already stored
            var existing = await transactionRepo.GetBySmsIdAsync(smsId);
            if (existing is not null) return;

            await transactionRepo.InsertAsync(transaction);

            // Auto-categorize the single new transaction
            if (categorizationService is not null)
                await categorizationService.CategorizeAsync([transaction]);

            // Update sync metadata
            if (syncMetaRepo is not null)
                await syncMetaRepo.UpdateAfterSyncAsync(smsId, timestampMs, newlyImportedCount: 1);
        }
        catch (Exception ex)
        {
            // Swallow — a BroadcastReceiver crash is silent and bad
            System.Diagnostics.Debug.WriteLine($"[MpesaSmsReceiver] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// PDU-based SmsMessage doesn't carry the inbox _id directly.
    /// We query the SMS inbox for a message near this timestamp to get
    /// the stable _id that matches what SmsReaderService uses.
    /// Falls back to timestamp as a synthetic ID if not found yet
    /// (Android may not have written it to the inbox yet at this point).
    /// </summary>
    private static long ResolveInboxSmsId(long timestampMs)
    {
        try
        {
            var contentUri = Android.Net.Uri.Parse("content://sms/inbox");
            if (contentUri is null) return -timestampMs;

            using var cursor = Android.App.Application.Context.ContentResolver?.Query(
                contentUri,
                projection: ["_id", "date"],
                // ±2s window to find the matching row
                selection: "date BETWEEN ? AND ?",
                selectionArgs: [(timestampMs - 2000).ToString(), (timestampMs + 2000).ToString()],
                sortOrder: "date DESC");

            if (cursor != null && cursor.MoveToFirst())
                return cursor.GetLong(cursor.GetColumnIndexOrThrow("_id"));
        }
        catch { /* fall through */ }

        // Synthetic fallback — negative to avoid colliding with real inbox IDs
        return -timestampMs;
    }
}