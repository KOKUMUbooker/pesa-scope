using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Services.Interfaces;
using PesaLens.Core.Models;

namespace PesaLens.App.ViewModels;

/// <summary>
/// Display wrapper around ExportHistory for the Recent Exports list.
/// </summary>
public class ExportHistoryRow
{
    public ExportHistory Source { get; init; } = null!;

    public string Title => Source.ReportKind switch
    {
        ReportKind.Transactions => "Transactions",
        ReportKind.SpendingSummary => "Spending Summary",
        ReportKind.BudgetCompliance => "Budget Compliance",
        _ => "Report"
    };

    public string FormatLabel => Source.ExportType == ExportType.Csv ? "CSV" : "PDF";

    // XAML picks the glyph via a DataTrigger on this — avoids hardcoding an
    // unverified MaterialSharp enum member name here.
    public bool IsCsv => Source.ExportType == ExportType.Csv;

    public string Subtitle =>
        $"{Source.StartDate:MMM d} – {Source.EndDate:MMM d, yyyy} · {Source.ExportedAt.ToLocalTime():MMM d, h:mm tt}";
}

public partial class ExportViewModel : ObservableObject
{
    private readonly IReportExportService _exportService;
    private readonly IExportHistoryRepository _exportHistoryRepo;
    private readonly IBudgetSnapshotRepository _budgetSnapshotRepo;

    // ── Transactions & Spending Summary date range ─────────────────────────────
    [ObservableProperty] private DateTime _rangeFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _rangeTo = DateTime.Today;

    // ── Budget Compliance picker state (mirrors BudgetHistoryViewModel) ───────
    [ObservableProperty] private List<int> _availableYears = [];
    [ObservableProperty] private List<string> _availableMonths = [];
    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private string _selectedMonthName = "All Months";
    [ObservableProperty] private bool _isPickerReady;

    private int? SelectedMonthIndex =>
        SelectedMonthName == "All Months" ? null : _availableMonths.IndexOf(SelectedMonthName);

    // ── Recent exports ──────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ExportHistoryRow> _recentExports = [];
    [ObservableProperty] private bool _hasRecentExports;

    // ── Busy state ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isExporting;

    public ExportViewModel(
        IReportExportService exportService,
        IExportHistoryRepository exportHistoryRepo,
        IBudgetSnapshotRepository budgetSnapshotRepo)
    {
        _exportService = exportService;
        _exportHistoryRepo = exportHistoryRepo;
        _budgetSnapshotRepo = budgetSnapshotRepo;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        IsPickerReady = false;

        try
        {
            var years = await _budgetSnapshotRepo.GetAvailableYearsAsync();

            // "Month" picker for budget compliance includes an "All Months" option
            AvailableMonths = new List<string> { "All Months" }
                .Concat(Enumerable.Range(1, 12).Select(m => new DateTime(2000, m, 1).ToString("MMMM")))
                .ToList();

            if (years.Count > 0)
            {
                AvailableYears = years;
                _selectedYear = years[0];
            }
            else
            {
                // No budget snapshots yet — still let the user pick the current year
                AvailableYears = [DateTime.Today.Year];
                _selectedYear = DateTime.Today.Year;
            }

            _selectedMonthName = "All Months";
            IsPickerReady = true;

            OnPropertyChanged(nameof(SelectedYear));
            OnPropertyChanged(nameof(SelectedMonthName));

            await LoadRecentExportsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRecentExportsAsync()
    {
        var recent = await _exportHistoryRepo.GetRecentAsync(10);
        RecentExports = new ObservableCollection<ExportHistoryRow>(
            recent.Select(e => new ExportHistoryRow { Source = e }));
        HasRecentExports = RecentExports.Count > 0;
    }

    // ── Transactions ──────────────────────────────────────────────────────────

    [RelayCommand]
    public Task ExportTransactionsCsvAsync() =>
        RunExportAsync(() => _exportService.ExportTransactionsCsvAsync(RangeFrom, RangeTo));

    [RelayCommand]
    public Task ExportTransactionsPdfAsync() =>
        RunExportAsync(() => _exportService.ExportTransactionsPdfAsync(RangeFrom, RangeTo));

    // ── Spending Summary ──────────────────────────────────────────────────────

    [RelayCommand]
    public Task ExportSpendingSummaryCsvAsync() =>
        RunExportAsync(() => _exportService.ExportSpendingSummaryCsvAsync(RangeFrom, RangeTo));

    [RelayCommand]
    public Task ExportSpendingSummaryPdfAsync() =>
        RunExportAsync(() => _exportService.ExportSpendingSummaryPdfAsync(RangeFrom, RangeTo));

    // ── Budget Compliance ─────────────────────────────────────────────────────

    [RelayCommand]
    public Task ExportBudgetComplianceCsvAsync() =>
        RunExportAsync(() => _exportService.ExportBudgetComplianceCsvAsync(SelectedYear, SelectedMonthIndex));

    [RelayCommand]
    public Task ExportBudgetCompliancePdfAsync() =>
        RunExportAsync(() => _exportService.ExportBudgetCompliancePdfAsync(SelectedYear, SelectedMonthIndex));

    // ── Share / re-share ──────────────────────────────────────────────────────

    [RelayCommand]
    public async Task ShareExistingAsync(ExportHistoryRow row)
    {
        if (row?.Source is null) return;
        var entry = row.Source;

        IsExporting = true;
        try
        {
            string path = entry.FilePath ?? string.Empty;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                // File no longer on disk (cache cleared) — regenerate from stored params
                path = await RegenerateAsync(entry);
            }

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share Report",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Share Failed", ex.Message, "OK");
        }
        finally
        {
            IsExporting = false;
        }
    }

    private Task<string> RegenerateAsync(ExportHistory entry) => (entry.ReportKind, entry.ExportType) switch
    {
        (ReportKind.Transactions, ExportType.Csv) => _exportService.ExportTransactionsCsvAsync(entry.StartDate, entry.EndDate),
        (ReportKind.Transactions, ExportType.Pdf) => _exportService.ExportTransactionsPdfAsync(entry.StartDate, entry.EndDate),
        (ReportKind.SpendingSummary, ExportType.Csv) => _exportService.ExportSpendingSummaryCsvAsync(entry.StartDate, entry.EndDate),
        (ReportKind.SpendingSummary, ExportType.Pdf) => _exportService.ExportSpendingSummaryPdfAsync(entry.StartDate, entry.EndDate),
        (ReportKind.BudgetCompliance, ExportType.Csv) => _exportService.ExportBudgetComplianceCsvAsync(entry.StartDate.Year, null),
        (ReportKind.BudgetCompliance, ExportType.Pdf) => _exportService.ExportBudgetCompliancePdfAsync(entry.StartDate.Year, null),
        _ => throw new InvalidOperationException("Unknown report kind/type combination.")
    };

    // ── Shared export runner ─────────────────────────────────────────────────

    private async Task RunExportAsync(Func<Task<string>> exportCall)
    {
        IsExporting = true;
        try
        {
            var path = await exportCall();

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share Report",
                File = new ShareFile(path)
            });

            await LoadRecentExportsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
        finally
        {
            IsExporting = false;
        }
    }
}