using SQLite;

namespace PesaLens.Core.Models;

/// <summary>
/// A single-row table that stores the user's global monthly spending limit.
/// Unlike Budget (which is per-category), this caps total spending across all categories.
/// You will only ever have one row — update it rather than inserting new rows.
/// </summary>
[Table("OverallBudget")]
public class OverallBudget
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [NotNull]
    [Column("monthly_limit")]
    public decimal MonthlyLimit { get; set; }

    [NotNull]
    [Column("notifications_enabled")]
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Stores the last month (yyyyMM) a warning notification was sent for this budget.
    /// Prevents repeat warning notifications within the same month once the
    /// WarningThresholdPercent has been crossed. Resets naturally on month rollover.
    /// </summary>
    [Column("last_warning_notified_month")]
    public int LastWarningNotifiedMonth { get; set; } = 0;

    /// <summary>
    /// Stores the last month (yyyyMM) an "exceeded" notification was sent for this budget.
    /// Prevents repeat exceeded notifications within the same month once spending
    /// has crossed MonthlyLimit. Resets naturally on month rollover.
    /// </summary>
    [Column("last_exceeded_notified_month")]
    public int LastExceededNotifiedMonth { get; set; } = 0;

    [NotNull]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}