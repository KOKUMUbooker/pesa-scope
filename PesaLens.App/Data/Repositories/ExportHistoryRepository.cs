using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using PesaLens.App.Repositories;
using SQLite;

namespace PesaLens.App.Repositories;

public class ExportHistoryRepository(SQLiteAsyncConnection db)
    : BaseRepository<ExportHistory>(db), IExportHistoryRepository
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
}