namespace PesaScope.App.Services.Interfaces;

public interface IReportExportService
{
    // ── Transactions ─────────────────────────────────────────────────────────
    Task<string> ExportTransactionsCsvAsync(DateTime from, DateTime to);
    Task<string> ExportTransactionsPdfAsync(DateTime from, DateTime to);

    // ── Spending Summary ─────────────────────────────────────────────────────
    Task<string> ExportSpendingSummaryCsvAsync(DateTime from, DateTime to);
    Task<string> ExportSpendingSummaryPdfAsync(DateTime from, DateTime to);

    // ── Budget Compliance ────────────────────────────────────────────────────
    // month = null exports every month available for that year
    Task<string> ExportBudgetComplianceCsvAsync(int year, int? month = null);
    Task<string> ExportBudgetCompliancePdfAsync(int year, int? month = null);
}