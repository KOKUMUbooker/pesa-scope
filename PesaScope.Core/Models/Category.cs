using SQLite;

namespace PesaScope.Core.Models;

[Table("Categories")]
public class Category
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [NotNull, Unique]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [NotNull]
    [Column("icon")]
    public string Icon { get; set; } = string.Empty;

    [NotNull]
    [Column("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// System categories are seeded on first launch and cannot be deleted by the user.
    /// </summary>
    [NotNull]
    [Column("is_system_category")]
    public bool IsSystemCategory { get; set; } = false;

    [NotNull]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation (not persisted) ────────────────────────────────────────────
    [Ignore]
    public List<Transaction> Transactions { get; set; } = [];

    [Ignore]
    public List<Budget> Budgets { get; set; } = [];

    [Ignore]
    public List<AutoCategorizationRule> Rules { get; set; } = [];
}