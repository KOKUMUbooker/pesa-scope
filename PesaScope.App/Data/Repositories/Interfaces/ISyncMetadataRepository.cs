using PesaLens.Core.Models;

namespace PesaLens.App.Data.Repositories.Interfaces;

public interface ISyncMetadataRepository
{
    /// <summary>
    /// Returns the single sync metadata row.
    /// Creates and returns a default row if none exists yet.
    /// </summary>
    Task<SyncMetadata> GetAsync();

    /// <summary>
    /// Updates the sync cursor after a successful import.
    /// </summary>
    Task<int> UpdateAfterSyncAsync(long lastSmsId, long lastSmsTimestamp, int newlyImportedCount);

    /// <summary>
    /// Resets sync metadata to defaults. Called when the user clears all app data.
    /// </summary>
    Task ResetAsync();
}