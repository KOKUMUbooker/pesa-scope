using SQLite;

namespace PesaLens.Core.Models;

[Table("Budgets")]
public class Budget
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key → Categories.id
    /// </summary>
    [NotNull, Indexed]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [NotNull]
    [Column("monthly_limit")]
    public decimal MonthlyLimit { get; set; }

    [NotNull]
    [Column("notifications_enabled")]
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Percentage of the budget (0–100) at which a warning notification fires.
    /// </summary>
    [NotNull]
    [Column("warning_threshold_percent")]
    public int WarningThresholdPercent { get; set; } = 80;

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

    // ── Navigation (not persisted) ────────────────────────────────────────────
    [Ignore]
    public Category? Category { get; set; }
}