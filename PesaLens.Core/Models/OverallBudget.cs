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

    [NotNull]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}