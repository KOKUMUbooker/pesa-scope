using SQLite;

namespace PesaLens.Core.Models;

/// <summary>
/// Single-row table for user preferences. Update in place; never insert a second row.
/// </summary>
[Table("AppSettings")]
public class AppSettings
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [NotNull]
    [Column("theme")]
    public AppTheme Theme { get; set; } = AppTheme.System;

    [NotNull]
    [Column("currency_display")]
    public CurrencyDisplay CurrencyDisplay { get; set; } = CurrencyDisplay.Ksh;

    [NotNull]
    [Column("budget_notifications_enabled")]
    public bool BudgetNotificationsEnabled { get; set; } = true;

    [NotNull]
    [Column("biometric_lock_enabled")]
    public bool BiometricLockEnabled { get; set; } = false;

    [NotNull]
    [Column("pin_lock_enabled")]
    public bool PinLockEnabled { get; set; } = false;

    [NotNull]
    [Column("onboarding_complete")]
    public bool OnboardingComplete { get; set; } = false;

    [NotNull]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum AppTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}

public enum CurrencyDisplay
{
    KES = 0,
    Ksh = 1
}