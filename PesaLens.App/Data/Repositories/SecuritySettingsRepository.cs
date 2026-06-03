using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using SQLite;

namespace PesaLens.App.Repositories;

public class SecuritySettingsRepository(SQLiteAsyncConnection db) : ISecuritySettingsRepository
{
    private readonly SQLiteAsyncConnection _db = db;

    public async Task<SecuritySettings> GetAsync()
    {
        var settings = await _db.Table<SecuritySettings>().FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new SecuritySettings();
            await _db.InsertAsync(settings);
        }

        return settings;
    }

    public async Task<int> UpdatePinHashAsync(string? pinHash)
    {
        var settings = await GetAsync();
        settings.PinHash = pinHash;
        settings.UpdatedAt = DateTime.UtcNow;
        return await _db.UpdateAsync(settings);
    }

    public async Task<int> SetBiometricsEnabledAsync(bool enabled)
    {
        var settings = await GetAsync();
        settings.BiometricsEnabled = enabled;
        settings.UpdatedAt = DateTime.UtcNow;
        return await _db.UpdateAsync(settings);
    }

    public async Task ResetAsync()
    {
        await _db.ExecuteAsync("DELETE FROM SecuritySettings");
        await _db.InsertAsync(new SecuritySettings());
    }
}