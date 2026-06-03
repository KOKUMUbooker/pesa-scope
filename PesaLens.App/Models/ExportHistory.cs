using SQLite;

namespace PesaLens.App.Models;

[Table("ExportHistory")]
public class ExportHistory
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [NotNull]
    [Column("export_type")]
    public ExportType ExportType { get; set; }

    [NotNull, Indexed]
    [Column("exported_at")]
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Start of the date range included in this export.
    /// </summary>
    [NotNull]
    [Column("start_date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End of the date range included in this export.
    /// </summary>
    [NotNull]
    [Column("end_date")]
    public DateTime EndDate { get; set; }
}

public enum ExportType
{
    Csv = 1,
    Pdf = 2
}