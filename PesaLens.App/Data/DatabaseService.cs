using PesaLens.App.Models;
using PesaLens.App.Repositories.Interfaces;
using SQLite;

namespace PesaLens.App.Repositories;

/// <summary>
/// Owns the single SQLiteAsyncConnection for the app.
/// Registered as a singleton in MauiProgram.cs.
/// Call InitializeAsync() once at startup before any repository is used.
/// </summary>
public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;

    public DatabaseService(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite |
                                                SQLiteOpenFlags.Create |
                                                SQLiteOpenFlags.SharedCache);
    }

    /// <summary>
    /// Creates all tables (if they don't exist) and returns the shared connection.
    /// Must be awaited before any repository operation.
    /// </summary>
    public async Task<SQLiteAsyncConnection> InitializeAsync()
    {
        await _db.CreateTablesAsync<
            Transaction,
            Category,
            AutoCategorizationRule,
            Budget,
            OverallBudget,
            SyncMetadata,
            AppSettings,
            SecuritySettings,
            ExportHistory>();

        return _db;
    }

    /// <summary>
    /// Drops and recreates all tables. Called when the user clears all app data.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await _db.DropTableAsync<Transaction>();
        await _db.DropTableAsync<Category>();
        await _db.DropTableAsync<AutoCategorizationRule>();
        await _db.DropTableAsync<Budget>();
        await _db.DropTableAsync<OverallBudget>();
        await _db.DropTableAsync<SyncMetadata>();
        await _db.DropTableAsync<AppSettings>();
        await _db.DropTableAsync<SecuritySettings>();
        await _db.DropTableAsync<ExportHistory>();

        await InitializeAsync();
    }
}