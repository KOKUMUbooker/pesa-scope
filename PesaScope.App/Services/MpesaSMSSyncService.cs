using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Services.Interfaces;

namespace PesaScope.App.Services;

public class MpesaSMSSyncService : IMpesaSMSSyncService
{
    private readonly ISyncMetadataRepository _syncMetadataRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISmsReaderService _smsReader;
    private readonly IMpesaSmsParser _mpesaSmsParser;
    private readonly IAutoCategorizationService _autoCategorizationService;

    public MpesaSMSSyncService(
        ISyncMetadataRepository syncMetadataRepo,
        ITransactionRepository transactionRepo,
        ISmsReaderService smsReader,
        IMpesaSmsParser mpesaSmsParser,
        IAutoCategorizationService autoCategorizationService)
    {
        _syncMetadataRepo = syncMetadataRepo;
        _transactionRepo = transactionRepo;
        _smsReader = smsReader;
        _mpesaSmsParser = mpesaSmsParser;
        _autoCategorizationService = autoCategorizationService;
    }

    public async Task<SyncResult> SyncAsync()
    {
        try
        {
            var hasPermission = await _smsReader.HasPermissionAsync();
            if (!hasPermission)
                return new SyncResult(false, 0, 0, NoPermission: true, Error: null);

            var syncMeta = await _syncMetadataRepo.GetAsync();
            var newMessages = await _smsReader.GetNewMpesaMessagesAsync(syncMeta.LastSmsId);

            if (newMessages is null || newMessages.Count == 0)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await _syncMetadataRepo.UpdateAfterSyncAsync(syncMeta.LastSmsId, nowMs, 0);
                return new SyncResult(true, 0, 0, false, null);
            }

            var transactions = new List<PesaScope.Core.Models.Transaction>();
            foreach (var msg in newMessages)
            {
                var tx = _mpesaSmsParser.Parse(msg.Body, msg.SmsId, msg.Timestamp);
                if (tx is not null) transactions.Add(tx);
            }

            var (inserted, duplicates) = await _transactionRepo.InsertManyAsync(transactions);
            await _autoCategorizationService.CategorizeAsync(transactions);

            var last = newMessages[^1];
            await _syncMetadataRepo.UpdateAfterSyncAsync(last.SmsId, last.Timestamp, inserted);

            return new SyncResult(true, inserted, duplicates, false, null);
        }
        catch (Exception ex)
        {
            return new SyncResult(false, 0, 0, false, ex.Message);
        }
    }
}