using PesaScope.App.Data.Repositories.Interfaces;

namespace PesaScope.App.Data;

/// <summary>
/// Handles all first-launch data seeding in the correct dependency order:
/// Categories must exist before Rules can reference them.
///
/// Usage in MauiProgram.cs (after DI is built):
/// <code>
///     var seeder = app.Services.GetRequiredService&lt;DatabaseSeeder&gt;();
///     await seeder.SeedAsync();
/// </code>
/// </summary>
public class DatabaseSeeder(
    IAppSettingsRepository appSettingsRepo,
    ISyncMetadataRepository syncMetadataRepo,
    ICategoryRepository categoryRepo,
    IAutoCategorizationRuleRepository ruleRepo)
{
    private readonly IAppSettingsRepository _appSettingsRepo = appSettingsRepo;
    private readonly ISyncMetadataRepository _syncMetadataRepo = syncMetadataRepo;
    private readonly ICategoryRepository _categoryRepo = categoryRepo;
    private readonly IAutoCategorizationRuleRepository _ruleRepo = ruleRepo;

    /// <summary>
    /// Runs all seeders in dependency order. Safe to call on every app launch —
    /// each seeder checks for existing data and skips if already seeded.
    /// </summary>
    public async Task SeedAsync()
    {
        // Step 1 — Singleton rows (AppSettings, SyncMetadata).
        // GetAsync() creates the row with defaults if it doesn't exist yet.
        await _appSettingsRepo.GetAsync();
        await _syncMetadataRepo.GetAsync();

        // Step 2 — Categories must be seeded before rules reference them.
        await _categoryRepo.SeedDefaultsAsync();

        // Step 3 — Auto-categorization rules (depend on category IDs from step 2).
        await _ruleRepo.SeedDefaultsAsync();
    }

    /// <summary>
    /// Wipes all data and re-seeds from scratch.
    /// Called from SettingsViewModel when the user chooses "Clear all data".
    /// </summary>
    public async Task ReseedAsync(DatabaseService databaseService)
    {
        await databaseService.ResetDatabaseAsync();
        await SeedAsync();
    }
}