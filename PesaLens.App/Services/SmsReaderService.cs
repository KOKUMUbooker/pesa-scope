using PesaLens.App.Services.Interfaces;

namespace PesaLens.App.Services;

/// <summary>
/// Android implementation of ISmsReaderService.
/// Queries the device SMS inbox via Android's ContentResolver.
/// Must only be called after READ_SMS permission has been granted.
/// </summary>
public class SmsReaderService : ISmsReaderService
{
    private const string MpesaSender = "MPESA";
    private const string SmsInboxUri = "content://sms/inbox";
    private static readonly string[] SmsColumns = ["_id", "date", "body"];

    public Task<bool> HasPermissionAsync() =>
        Permissions.CheckStatusAsync<Permissions.Sms>()
                   .ContinueWith(t => t.Result == PermissionStatus.Granted);

    public Task<List<PesaLens.App.Services.Interfaces.SmsMessage>> GetAllMpesaMessagesAsync() =>
        Task.Run(() => QueryInbox(selection: "address = ?",
                                  selectionArgs: [MpesaSender],
                                  sortOrder: "_id ASC"));

    public Task<List<PesaLens.App.Services.Interfaces.SmsMessage>> GetNewMpesaMessagesAsync(long lastSmsId) =>
        Task.Run(() => QueryInbox(selection: "address = ? AND _id > ?",
                                  selectionArgs: [MpesaSender, lastSmsId.ToString()],
                                  sortOrder: "_id ASC"));

    // ── Private ───────────────────────────────────────────────────────────────

    private static List<PesaLens.App.Services.Interfaces.SmsMessage> QueryInbox(
        string selection,
        string[] selectionArgs,
        string sortOrder)
    {
        var results = new List<PesaLens.App.Services.Interfaces.SmsMessage>();
        var context = Android.App.Application.Context;
        var contentUri = Android.Net.Uri.Parse(SmsInboxUri);

        if (contentUri == null) return [];

        using var cursor = context.ContentResolver?.Query(
            contentUri,
            projection: SmsColumns,
            selection: selection,
            selectionArgs: selectionArgs,
            sortOrder: sortOrder);

        if (cursor is null || cursor.Count == 0)
            return results;

        int idIndex = cursor.GetColumnIndexOrThrow("_id");
        int dateIndex = cursor.GetColumnIndexOrThrow("date");
        int bodyIndex = cursor.GetColumnIndexOrThrow("body");

        while (cursor.MoveToNext())
        {
            var body = cursor.GetString(bodyIndex);

            if (string.IsNullOrWhiteSpace(body))
                continue;

            results.Add(new PesaLens.App.Services.Interfaces.SmsMessage(
                SmsId: cursor.GetLong(idIndex),
                Timestamp: cursor.GetLong(dateIndex),
                Body: body));
        }

        return results;
    }
}