using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.ViewModels;

public partial class RuleImportViewModel : ObservableObject
{
    private readonly IRuleExportService _exportService;
    private readonly IPendingRuleImportSession _session;

    private RuleImportPreview? _preview;
    private List<RuleImportRowItem> _allRows = [];

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isCommitting;
    [ObservableProperty] private bool _hasPreview;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedCategoryFilter = "All categories";

    public ObservableCollection<RuleImportRowItem> Rows { get; } = [];
    public ObservableCollection<string> FilterCategories { get; } = [];

    public int SelectableCount => _allRows.Count(r => r.CanSelect);
    public int SelectedCount => _allRows.Count(r => r.CanSelect && r.IsSelected);

    public string SummaryText =>
        _preview is null
            ? string.Empty
            : $"{_preview.NewCount} new · {_preview.ModifiedCount} modified · " +
              $"{_preview.DuplicateCount} duplicate · {_preview.UnresolvedCount} unresolved";

    public string ImportButtonText => $"Import Selected ({SelectedCount})";

    public event Action? RequestClose;

    public RuleImportViewModel(IRuleExportService exportService, IPendingRuleImportSession session)
    {
        _exportService = exportService;
        _session = session;
    }

    [RelayCommand]
    public void Load()
    {
        _preview = _session.TakePreview();
        IsLoading = false;

        if (_preview is null)
        {
            HasPreview = false;
            return;
        }

        HasPreview = true;

        _allRows = _preview.Items.Select(item => new RuleImportRowItem(item)).ToList();
        foreach (var row in _allRows)
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(RuleImportRowItem.IsSelected))
                    RaiseSelectionChanged();
            };

        var categories = _allRows
            .Select(r => r.CategoryDisplayName)
            .Distinct()
            .OrderBy(n => n);

        FilterCategories.Clear();
        FilterCategories.Add("All categories");
        foreach (var c in categories)
            FilterCategories.Add(c);
        SelectedCategoryFilter = FilterCategories[0];

        ApplyFilter();
        RaiseSelectionChanged();
        OnPropertyChanged(nameof(SummaryText));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(r =>
                r.Item.Incoming.MatchValue.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (SelectedCategoryFilter != "All categories")
            filtered = filtered.Where(r => r.CategoryDisplayName == SelectedCategoryFilter);

        filtered = filtered.OrderBy(r => StatusSortOrder(r.Status));

        Rows.Clear();
        foreach (var row in filtered)
            Rows.Add(row);
    }

    private static int StatusSortOrder(RuleImportStatus status) => status switch
    {
        RuleImportStatus.New => 0,
        RuleImportStatus.Modified => 1,
        RuleImportStatus.Duplicate => 2,
        RuleImportStatus.CategoryNotFound => 3,
        _ => 4
    };
    [RelayCommand]
    private void SelectAllSelectable()
    {
        foreach (var row in _allRows.Where(r => r.CanSelect))
            row.IsSelected = true;
        RaiseSelectionChanged();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var row in _allRows)
            row.IsSelected = false;
        RaiseSelectionChanged();
    }

    private void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(ImportButtonText));
    }

    [RelayCommand]
    public async Task CommitAsync()
    {
        if (_preview is null || IsCommitting) return;

        var accepted = _allRows.Where(r => r.CanSelect && r.IsSelected).Select(r => r.Item).ToList();
        if (accepted.Count == 0) return;

        IsCommitting = true;
        try
        {
            var result = await _exportService.CommitAsync(_preview, accepted);

            var skippedByChoice = _allRows.Count(r => r.CanSelect && !r.IsSelected);
            var notSelectable = _allRows.Count(r => !r.CanSelect);

            var message =
                $"Added {result.Added} new rule{(result.Added == 1 ? "" : "s")}.\n" +
                $"Updated {result.Updated} modified rule{(result.Updated == 1 ? "" : "s")}.\n" +
                (skippedByChoice > 0 ? $"Left out {skippedByChoice} unchecked.\n" : "") +
                (notSelectable > 0 ? $"Skipped {notSelectable} (duplicates or unresolved categories)." : "");

            await Shell.Current.DisplayAlertAsync("Import Complete", message.Trim(), "OK");
            RequestClose?.Invoke();
        }
        finally
        {
            IsCommitting = false;
        }
    }
}

/// <summary>
/// UI wrapper around a RuleImportPreviewItem — adds a category display name
/// (works for both New/Duplicate rows via ResolvedCategoryId's name, and
/// CategoryNotFound rows via the raw incoming name) and a CanSelect flag.
/// </summary>
public partial class RuleImportRowItem : ObservableObject
{
    public RuleImportPreviewItem Item { get; }

    public RuleImportStatus Status => Item.Status;
    public bool CanSelect => Status is RuleImportStatus.New or RuleImportStatus.Modified;

    public string CategoryDisplayName => Item.Incoming.CategoryName;

    public string StatusLabel => Status switch
    {
        RuleImportStatus.New => "New",
        RuleImportStatus.Modified => "Modified",
        RuleImportStatus.Duplicate => "Duplicate",
        RuleImportStatus.CategoryNotFound => "Category not found",
        _ => ""
    };

    [ObservableProperty] private bool _isSelected;

    public RuleImportRowItem(RuleImportPreviewItem item)
    {
        Item = item;
        _isSelected = item.IsSelected;
    }
}