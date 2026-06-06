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

    [NotNull]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation (not persisted) ────────────────────────────────────────────
    [Ignore]
    public Category? Category { get; set; }
}