using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories.Interfaces;

public interface IExportHistoryRepository : IRepository<ExportHistory>
{
    /// <summary>
    /// Returns export history entries newest first.
    /// </summary>
    Task<List<ExportHistory>> GetRecentAsync(int count = 20);

    /// <summary>
    /// Returns all exports of a specific type.
    /// </summary>
    Task<List<ExportHistory>> GetByTypeAsync(ExportType exportType);

    Task ClearAllAsync();

}