using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using SQLite;

namespace PesaLens.App.Data.Repositories;

public class AppSettingsRepository(SQLiteAsyncConnection db) : IAppSettingsRepository
{
    private readonly SQLiteAsyncConnection _db = db;

    public async Task<AppSettings> GetAsync()
    {
        var settings = await _db.Table<AppSettings>().FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new AppSettings();
            await _db.InsertAsync(settings);
        }

        return settings;
    }

    public async Task<int> UpdateAsync(AppSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        return await _db.UpdateAsync(settings);
    }

    public async Task ResetAsync()
    {
        await _db.ExecuteAsync("DELETE FROM AppSettings");
        await _db.InsertAsync(new AppSettings());
    }
}