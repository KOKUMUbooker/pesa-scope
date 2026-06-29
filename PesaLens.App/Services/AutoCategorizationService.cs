using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Services.Interfaces;
using PesaLens.Core.Models;

public class AutoCategorizationService : IAutoCategorizationService
{
    private readonly IAutoCategorizationRuleRepository _rulesRepo;
    private readonly ITransactionRepository _transactionRepo;

    public AutoCategorizationService(
        IAutoCategorizationRuleRepository rulesRepo,
        ITransactionRepository transactionRepo)
    {
        _rulesRepo = rulesRepo;
        _transactionRepo = transactionRepo;
    }

    public async Task CategorizeAsync(IList<Transaction> transactions)
    {
        // Fetch once, sorted by priority descending — highest priority wins
        var rules = await _rulesRepo.GetEnabledOrderedByPriorityAsync();
        var toUpdate = new List<Transaction>();

        foreach (var tx in transactions)
        {
            if (tx.CategoryId != 0) continue; // already categorized, skip

            var matched = rules.FirstOrDefault(r => Matches(tx, r));
            if (matched is null) continue;

            tx.CategoryId = matched.CategoryId; // CategoryId from the rule
            toUpdate.Add(tx);
        }

        if (toUpdate.Count > 0)
            await _transactionRepo.UpdateManyAsync(toUpdate);
    }

    public async Task<int?> CategorizeAndGetCategoryIdAsync(Transaction transaction)
    {
        if (transaction.CategoryId != 0)
            return transaction.CategoryId; // already categorized

        var rules = await _rulesRepo.GetEnabledOrderedByPriorityAsync();
        var matched = rules.FirstOrDefault(r => Matches(transaction, r));
        if (matched is null) return null;

        transaction.CategoryId = matched.CategoryId;
        await _transactionRepo.UpdateManyAsync([transaction]);

        return matched.CategoryId;
    }

    private static bool Matches(Transaction tx, AutoCategorizationRule rule) =>
        rule.RuleType switch
        {
            RuleType.ContainsText =>
                tx.CounterpartyName.Contains(rule.MatchValue, StringComparison.OrdinalIgnoreCase),

            RuleType.MerchantName =>
                tx.CounterpartyName.Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase),

            RuleType.PaybillNumber =>
                tx.CounterpartyNumber?.Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase) ?? false,

            RuleType.TillNumber =>
                tx.CounterpartyNumber?.Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase) ?? false,

            RuleType.TransactionType =>
                tx.Type.ToString().Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase),

            _ => false
        };
}