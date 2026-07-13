using SQLite;

namespace PesaLens.Core.Models;

[Table("BudgetSnapshots")]
public class BudgetSnapshot
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Null = overall budget snapshot. Non-null = per-category snapshot.
    /// </summary>
    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Column("category_name")]
    public string? CategoryName { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("limit")]
    public decimal Limit { get; set; }

    [Column("spent")]
    public decimal Spent { get; set; }

    [Column("was_exceeded")]
    public bool WasExceeded { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}