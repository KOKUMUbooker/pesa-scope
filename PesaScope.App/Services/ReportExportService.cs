using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using IContainer = QuestPDF.Infrastructure.IContainer;
using Colors = QuestPDF.Helpers.Colors;

namespace PesaScope.App.Services;

public class ReportExportService(
    ITransactionRepository transactionRepo,
    ICategoryRepository categoryRepo,
    IBudgetSnapshotRepository budgetSnapshotRepo,
    IAppSettingsRepository appSettingsRepo,
    IExportHistoryRepository exportHistoryRepo) : IReportExportService
{
    private const string LogoLogicalName = "logo_rounded_mini.png";

    // ════════════════════════════════════════════════════════════════════════
    // Transactions
    // ════════════════════════════════════════════════════════════════════════

    public async Task<string> ExportTransactionsCsvAsync(DateTime from, DateTime to)
    {
        var transactions = await transactionRepo.GetFilteredAsync(from, to);
        var categories = await categoryRepo.GetAllActiveAsync();
        var categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);

        var path = BuildFilePath("Transactions", "csv");

        await using var writer = new StreamWriter(path);
        await using var csv = new CsvWriter(writer, CsvConfigInvariant());

        csv.WriteHeader<TransactionCsvRow>();
        await csv.NextRecordAsync();

        foreach (var tx in transactions.OrderByDescending(t => t.TransactionDate))
        {
            csv.WriteRecord(new TransactionCsvRow(
                tx.TransactionDate,
                tx.Type.ToString(),
                tx.Amount,
                tx.CounterpartyName,
                categoryNames.GetValueOrDefault(tx.CategoryId, "Uncategorized"),
                tx.Note ?? string.Empty,
                tx.MpesaCode,
                tx.BalanceAfterTransaction));
            await csv.NextRecordAsync();
        }

        await LogExportAsync(ReportKind.Transactions, ExportType.Csv, from, to, path);
        return path;
    }

    public async Task<string> ExportTransactionsPdfAsync(DateTime from, DateTime to)
    {
        var transactions = await transactionRepo.GetFilteredAsync(from, to);
        var categories = await categoryRepo.GetAllActiveAsync();
        var categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);
        var currency = await GetCurrencySymbolAsync();
        var logo = await LoadLogoBytesAsync();

        var path = BuildFilePath("Transactions", "pdf");
        var ordered = transactions.OrderByDescending(t => t.TransactionDate).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, logo,
                    "Transactions Report",
                    $"{from:MMM d, yyyy} – {to:MMM d, yyyy}"));

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Date
                        columns.RelativeColumn(2); // Type
                        columns.RelativeColumn(3); // Counterparty
                        columns.RelativeColumn(2); // Category
                        columns.RelativeColumn(2); // Amount
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Date");
                        header.Cell().Element(HeaderCell).Text("Type");
                        header.Cell().Element(HeaderCell).Text("Counterparty");
                        header.Cell().Element(HeaderCell).Text("Category");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                    });

                    foreach (var tx in ordered)
                    {
                        bool isInflow = tx.Type == TransactionType.ReceiveMoney || tx.Type == TransactionType.Deposit;

                        table.Cell().Element(BodyCell).Text(tx.TransactionDate.ToString("MMM d, yyyy"));
                        table.Cell().Element(BodyCell).Text(tx.Type.ToString());
                        table.Cell().Element(BodyCell).Text(tx.CounterpartyName);
                        table.Cell().Element(BodyCell).Text(categoryNames.GetValueOrDefault(tx.CategoryId, "Uncategorized"));
                        table.Cell().Element(BodyCell).AlignRight().Text($"{(isInflow ? "+" : "-")}{currency} {tx.Amount:N2}")
                            .FontColor(isInflow ? Colors.Green.Darken1 : Colors.Red.Darken1);
                    }
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        document.GeneratePdf(path);
        await LogExportAsync(ReportKind.Transactions, ExportType.Pdf, from, to, path);
        return path;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Spending Summary
    // ════════════════════════════════════════════════════════════════════════

    public async Task<string> ExportSpendingSummaryCsvAsync(DateTime from, DateTime to)
    {
        var (rows, _, _) = await BuildSpendingSummaryAsync(from, to);
        var path = BuildFilePath("SpendingSummary", "csv");

        await using var writer = new StreamWriter(path);
        await using var csv = new CsvWriter(writer, CsvConfigInvariant());

        csv.WriteHeader<SpendingSummaryCsvRow>();
        await csv.NextRecordAsync();

        foreach (var row in rows)
        {
            csv.WriteRecord(row);
            await csv.NextRecordAsync();
        }

        await LogExportAsync(ReportKind.SpendingSummary, ExportType.Csv, from, to, path);
        return path;
    }

    public async Task<string> ExportSpendingSummaryPdfAsync(DateTime from, DateTime to)
    {
        var (rows, totalSpent, totalReceived) = await BuildSpendingSummaryAsync(from, to);
        var currency = await GetCurrencySymbolAsync();
        var logo = await LoadLogoBytesAsync();

        var path = BuildFilePath("SpendingSummary", "pdf");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, logo,
                    "Spending Summary",
                    $"{from:MMM d, yyyy} – {to:MMM d, yyyy}"));

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => TotalsCard(c, "Total Spent", totalSpent, currency, Colors.Red.Darken1));
                        row.ConstantItem(10);
                        row.RelativeItem().Element(c => TotalsCard(c, "Total Received", totalReceived, currency, Colors.Green.Darken1));
                    });

                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Category
                            columns.RelativeColumn(2); // Amount
                            columns.RelativeColumn(2); // % of total
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Category");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Spent");
                            header.Cell().Element(HeaderCell).AlignRight().Text("% of Total");
                        });

                        foreach (var row in rows.OrderByDescending(r => r.TotalSpent))
                        {
                            table.Cell().Element(BodyCell).Text(row.Category);
                            table.Cell().Element(BodyCell).AlignRight().Text($"{currency} {row.TotalSpent:N2}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{row.PercentOfTotal:N1}%");
                        }
                    });
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        document.GeneratePdf(path);
        await LogExportAsync(ReportKind.SpendingSummary, ExportType.Pdf, from, to, path);
        return path;
    }

    private async Task<(List<SpendingSummaryCsvRow> Rows, decimal TotalSpent, decimal TotalReceived)> BuildSpendingSummaryAsync(DateTime from, DateTime to)
    {
        var spendingByCategory = await transactionRepo.GetSpendingByCategoryAsync(from, to);
        var totalSpent = await transactionRepo.GetTotalSpentAsync(from, to);
        var totalReceived = await transactionRepo.GetTotalReceivedAsync(from, to);
        var categories = await categoryRepo.GetAllActiveAsync();
        var categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);

        var rows = spendingByCategory
            .Select(kvp => new SpendingSummaryCsvRow(
                categoryNames.GetValueOrDefault(kvp.Key, "Uncategorized"),
                kvp.Value,
                totalSpent == 0 ? 0 : Math.Round(kvp.Value / totalSpent * 100, 1)))
            .OrderByDescending(r => r.TotalSpent)
            .ToList();

        return (rows, totalSpent, totalReceived);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Budget Compliance
    // ════════════════════════════════════════════════════════════════════════

    public async Task<string> ExportBudgetComplianceCsvAsync(int year, int? month = null)
    {
        var snapshots = await GetSnapshotsForRangeAsync(year, month);
        var path = BuildFilePath("BudgetCompliance", "csv");

        await using var writer = new StreamWriter(path);
        await using var csv = new CsvWriter(writer, CsvConfigInvariant());

        csv.WriteHeader<BudgetComplianceCsvRow>();
        await csv.NextRecordAsync();

        foreach (var s in snapshots)
        {
            csv.WriteRecord(new BudgetComplianceCsvRow(
                s.Year,
                s.Month,
                s.CategoryName ?? "Overall",
                s.Limit,
                s.Spent,
                s.Limit == 0 ? 0 : Math.Round(s.Spent / s.Limit * 100, 1),
                s.WasExceeded));
            await csv.NextRecordAsync();
        }

        var (from, to) = MonthRangeBounds(year, month);
        await LogExportAsync(ReportKind.BudgetCompliance, ExportType.Csv, from, to, path);
        return path;
    }

    public async Task<string> ExportBudgetCompliancePdfAsync(int year, int? month = null)
    {
        var snapshots = await GetSnapshotsForRangeAsync(year, month);
        var currency = await GetCurrencySymbolAsync();
        var logo = await LoadLogoBytesAsync();

        var path = BuildFilePath("BudgetCompliance", "pdf");
        var subtitle = month is null ? $"Year {year}" : $"{new DateTime(year, month.Value, 1):MMMM yyyy}";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, logo, "Budget Compliance Report", subtitle));

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Period
                        columns.RelativeColumn(2); // Category
                        columns.RelativeColumn(2); // Limit
                        columns.RelativeColumn(2); // Spent
                        columns.RelativeColumn(1); // %
                        columns.RelativeColumn(2); // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Period");
                        header.Cell().Element(HeaderCell).Text("Category");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Limit");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Spent");
                        header.Cell().Element(HeaderCell).AlignRight().Text("%");
                        header.Cell().Element(HeaderCell).Text("Status");
                    });

                    foreach (var s in snapshots.OrderBy(s => s.Year).ThenBy(s => s.Month).ThenBy(s => s.CategoryName))
                    {
                        var pct = s.Limit == 0 ? 0 : Math.Round(s.Spent / s.Limit * 100, 1);

                        table.Cell().Element(BodyCell).Text($"{new DateTime(s.Year, s.Month, 1):MMM yyyy}");
                        table.Cell().Element(BodyCell).Text(s.CategoryName ?? "Overall");
                        table.Cell().Element(BodyCell).AlignRight().Text($"{currency} {s.Limit:N0}");
                        table.Cell().Element(BodyCell).AlignRight().Text($"{currency} {s.Spent:N0}");
                        table.Cell().Element(BodyCell).AlignRight().Text($"{pct:N0}%");
                        table.Cell().Element(BodyCell)
                            .Text(s.WasExceeded ? "Exceeded" : "Within Budget")
                            .FontColor(s.WasExceeded ? Colors.Red.Darken1 : Colors.Green.Darken1);
                    }
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        document.GeneratePdf(path);
        var (from, to) = MonthRangeBounds(year, month);
        await LogExportAsync(ReportKind.BudgetCompliance, ExportType.Pdf, from, to, path);
        return path;
    }

    private async Task<List<BudgetSnapshot>> GetSnapshotsForRangeAsync(int year, int? month)
    {
        if (month is not null)
            return await budgetSnapshotRepo.GetByMonthAsync(year, month.Value);

        var all = new List<BudgetSnapshot>();
        for (int m = 1; m <= 12; m++)
            all.AddRange(await budgetSnapshotRepo.GetByMonthAsync(year, m));

        return all;
    }

    private static (DateTime From, DateTime To) MonthRangeBounds(int year, int? month)
    {
        if (month is null)
            return (new DateTime(year, 1, 1), new DateTime(year, 12, DateTime.DaysInMonth(year, 12)));

        var from = new DateTime(year, month.Value, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Shared helpers
    // ════════════════════════════════════════════════════════════════════════

    private static CsvConfiguration CsvConfigInvariant() =>
        new(CultureInfo.InvariantCulture);

    private static string GetExportsDirectory()
    {
        var dir = Path.Combine(FileSystem.CacheDirectory, "exports");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string BuildFilePath(string prefix, string extension) =>
        Path.Combine(GetExportsDirectory(), $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");

    private async Task<string> GetCurrencySymbolAsync()
    {
        var settings = await appSettingsRepo.GetAsync();
        return settings.CurrencyDisplay == CurrencyDisplay.Ksh ? "Ksh" : "KES";
    }

    private static async Task<byte[]?> LoadLogoBytesAsync()
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(LogoLogicalName);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch
        {
            // Logo missing or not registered as a MauiAsset — degrade gracefully, no logo in PDF.
            return null;
        }
    }

    private async Task LogExportAsync(ReportKind kind, ExportType type, DateTime from, DateTime to, string path)
    {
        await exportHistoryRepo.InsertAsync(new ExportHistory
        {
            ReportKind = kind,
            ExportType = type,
            StartDate = from,
            EndDate = to,
            FilePath = path,
            ExportedAt = DateTime.UtcNow
        });
    }

    // ── QuestPDF composition helpers ─────────────────────────────────────────

    private static void ComposeHeader(IContainer container, byte[]? logo, string title, string subtitle)
    {
        container.Row(row =>
        {
            if (logo is not null)
            {
                row.ConstantItem(40).Height(40).Image(logo).FitArea();
                row.ConstantItem(10);
            }

            row.RelativeItem().Column(col =>
            {
                col.Item().Text("PesaScope").FontSize(11).FontColor(Colors.Grey.Darken1);
                col.Item().Text(title).FontSize(16).Bold();
                col.Item().Text(subtitle).FontSize(10).FontColor(Colors.Grey.Darken2);
            });

            row.ConstantItem(100).AlignRight().Text($"Generated\n{DateTime.Now:MMM d, yyyy}")
                .FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("PesaScope — Page ").FontSize(8).FontColor(Colors.Grey.Medium);
            text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
            text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
            text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private static void TotalsCard(IContainer container, string label, decimal amount, string currency, string color)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
        {
            col.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(4).Text($"{currency} {amount:N2}").FontSize(16).Bold().FontColor(color);
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.DefaultTextStyle(x => x.Bold().FontColor(Colors.White))
                  .Background(Colors.Grey.Darken3)
                  .Padding(6);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(6);

    // ── CSV row DTOs ──────────────────────────────────────────────────────────

    private record TransactionCsvRow(
        DateTime Date,
        string Type,
        decimal Amount,
        string Counterparty,
        string Category,
        string Note,
        string MpesaCode,
        decimal BalanceAfterTransaction);

    private record SpendingSummaryCsvRow(
        string Category,
        decimal TotalSpent,
        decimal PercentOfTotal);

    private record BudgetComplianceCsvRow(
        int Year,
        int Month,
        string Category,
        decimal Limit,
        decimal Spent,
        decimal PercentUsed,
        bool WasExceeded);
}