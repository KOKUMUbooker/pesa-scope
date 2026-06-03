using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using SQLite;

namespace PesaLens.App.Repositories;

public class SyncMetadataRepository(SQLiteAsyncConnection db) : ISyncMetadataRepository
{
    private readonly SQLiteAsyncConnection _db = db;

    public async Task<SyncMetadata> GetAsync()
    {
        var metadata = await _db.Table<SyncMetadata>().FirstOrDefaultAsync();

        if (metadata is null)
        {
            metadata = new SyncMetadata();
            await _db.InsertAsync(metadata);
        }

        return metadata;
    }

    public async Task<int> UpdateAfterSyncAsync(
        long lastSmsId,
        long lastSmsTimestamp,
        int newlyImportedCount)
    {
        var metadata = await GetAsync();

        metadata.LastSmsId = lastSmsId;
        metadata.LastSmsTimestamp = lastSmsTimestamp;
        metadata.LastSyncTime = DateTime.UtcNow;
        metadata.ImportedTransactionCount += newlyImportedCount;

        return await _db.UpdateAsync(metadata);
    }

    public async Task ResetAsync()
    {
        await _db.ExecuteAsync("DELETE FROM SyncMetadata");
        await _db.InsertAsync(new SyncMetadata());
    }
}