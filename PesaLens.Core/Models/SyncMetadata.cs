using SQLite;

namespace PesaLens.Core.Models;

/// <summary>
/// Single-row table that tracks the state of the last SMS import.
/// Used to efficiently query only new messages on subsequent syncs
/// instead of re-reading the entire SMS inbox each time.
/// </summary>
[Table("SyncMetadata")]
public class SyncMetadata
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// The Android SMS inbox row ID of the last successfully imported message.
    /// On next sync, query WHERE sms._id > LastSmsId to get only new messages.
    /// </summary>
    [NotNull]
    [Column("last_sms_id")]
    public long LastSmsId { get; set; } = 0;

    /// <summary>
    /// The timestamp (ms since epoch) of the last imported SMS.
    /// Secondary check alongside LastSmsId for robustness.
    /// </summary>
    [NotNull]
    [Column("last_sms_timestamp")]
    public long LastSmsTimestamp { get; set; } = 0;

    /// <summary>
    /// Wall-clock time of the last successful sync. Displayed in the UI.
    /// </summary>
    [NotNull]
    [Column("last_sync_time")]
    public DateTime LastSyncTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Cumulative count of all transactions ever imported into PesaLens.
    /// </summary>
    [NotNull]
    [Column("imported_transaction_count")]
    public int ImportedTransactionCount { get; set; } = 0;
}