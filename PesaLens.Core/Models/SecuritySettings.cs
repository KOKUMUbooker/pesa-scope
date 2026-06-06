using SQLite;

namespace PesaLens.Core.Models;

/// <summary>
/// Single-row table for security credentials.
/// Kept separate from AppSettings so PIN/biometric data
/// can be handled with stricter access patterns in the service layer.
/// </summary>
[Table("SecuritySettings")]
public class SecuritySettings
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// BCrypt or PBKDF2 hash of the user's PIN. Never store the raw PIN.
    /// Null if PIN lock has never been set up.
    /// </summary>
    [Column("pin_hash")]
    public string? PinHash { get; set; }

    [NotNull]
    [Column("biometrics_enabled")]
    public bool BiometricsEnabled { get; set; } = false;

    [NotNull]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}