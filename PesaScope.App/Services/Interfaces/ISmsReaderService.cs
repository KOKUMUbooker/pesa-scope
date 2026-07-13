namespace PesaScope.App.Services.Interfaces;

/// <summary>
/// Abstracts Android SMS inbox access so the rest of the app
/// never touches platform-specific ContentResolver code directly.
/// </summary>
public interface ISmsReaderService
{
    /// <summary>
    /// Returns all SMS messages from MPESA in the inbox, ordered by _id ascending.
    /// </summary>
    Task<List<SmsMessage>> GetAllMpesaMessagesAsync();

    /// <summary>
    /// Returns only messages with an Android SMS _id greater than <paramref name="lastSmsId"/>.
    /// Used for incremental sync after the initial import.
    /// </summary>
    Task<List<SmsMessage>> GetNewMpesaMessagesAsync(long lastSmsId);

    /// <summary>
    /// Returns true if the READ_SMS permission is currently granted.
    /// </summary>
    Task<bool> HasPermissionAsync();
}

/// <summary>
/// A raw SMS message read from the Android inbox.
/// </summary>
public sealed record SmsMessage(
    long SmsId,
    long Timestamp,
    string Body);