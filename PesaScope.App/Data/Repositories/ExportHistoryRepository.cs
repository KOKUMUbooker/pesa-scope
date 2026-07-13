using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.Core.Models;

namespace PesaLens.App.Data.Repositories;

public class ExportHistoryRepository(DatabaseService databaseService)
    : BaseRepository<ExportHistory>(databaseService), IExportHistoryRepository
{
    public Task<List<ExportHistory>> GetRecentAsync(int count = 20) =>
        _db.Table<ExportHistory>()
           .OrderByDescending(e => e.ExportedAt)
           .Take(count)
           .ToListAsync();

    public Task<List<ExportHistory>> GetByTypeAsync(ExportType exportType) =>
        _db.Table<ExportHistory>()
           .Where(e => e.ExportType == exportType)
           .OrderByDescending(e => e.ExportedAt)
           .ToListAsync();

    public Task ClearAllAsync() =>
        _db.DeleteAllAsync<ExportHistory>();
}
