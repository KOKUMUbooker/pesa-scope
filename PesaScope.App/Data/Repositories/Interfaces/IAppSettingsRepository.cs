using PesaLens.Core.Models;

namespace PesaLens.App.Data.Repositories.Interfaces;

public interface IAppSettingsRepository
{
    /// <summary>
    /// Returns the single app settings row.
    /// Creates and returns defaults if the row doesn't exist yet.
    /// </summary>
    Task<AppSettings> GetAsync();

    /// <summary>
    /// Persists the updated settings row.
    /// </summary>
    Task<int> UpdateAsync(AppSettings settings);

    /// <summary>
    /// Resets all settings to their defaults.
    /// </summary>
    Task ResetAsync();
}