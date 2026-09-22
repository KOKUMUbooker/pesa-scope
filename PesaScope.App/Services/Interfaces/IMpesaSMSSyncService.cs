namespace PesaScope.App.Services.Interfaces;

public record SyncResult(bool Success, int Inserted, int Duplicates, bool NoPermission, string? Error);

public interface IMpesaSMSSyncService
{
    Task<SyncResult> SyncAsync();
}