using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;
using CommunityToolkit.Maui.Storage;

namespace PesaScope.App.ViewModels;

public partial class RuleExportViewModel : ObservableObject
{
    private readonly IAutoCategorizationRuleRepository _ruleRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IRuleExportService _exportService;

    private List<RuleSelectionItem> _allItems = [];

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Category? _selectedCategoryFilter;

    public ObservableCollection<RuleSelectionItem> Rules { get; } = [];
    public ObservableCollection<Category> FilterCategories { get; } = [];

    public int SelectedCount => _allItems.Count(r => r.IsSelected);
    public string ExportButtonText => $"Export Selected ({SelectedCount})";

    public RuleExportViewModel(
        IAutoCategorizationRuleRepository ruleRepo,
        ICategoryRepository categoryRepo,
        IRuleExportService exportService)
    {
        _ruleRepo = ruleRepo;
        _categoryRepo = categoryRepo;
        _exportService = exportService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var categories = await _categoryRepo.GetAllAsync();
            var rules = await _ruleRepo.GetAllAsync();

            FilterCategories.Clear();
            FilterCategories.Add(new Category { Id = 0, Name = "All categories" });
            foreach (var c in categories.OrderBy(c => c.Name))
                FilterCategories.Add(c);
            SelectedCategoryFilter = FilterCategories[0];

            var categoryById = categories.ToDictionary(c => c.Id);

            _allItems = rules.Select(r => new RuleSelectionItem
            {
                Rule = r,
                CategoryName = categoryById.TryGetValue(r.CategoryId, out var cat) ? cat.Name : "Uncategorized",
                IsSelected = true
            }).ToList();

            foreach (var item in _allItems)
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(RuleSelectionItem.IsSelected))
                        RaiseSelectionChanged();
                    // ExportButtonText is bound via SelectedCount notification below
                };

            ApplyFilter();
            RaiseSelectionChanged();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Load Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryFilterChanged(Category? value) => ApplyFilter();

    private void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(ExportButtonText));
    }

    private void ApplyFilter()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(r =>
                r.Rule.MatchValue.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (SelectedCategoryFilter is { Id: not 0 })
            filtered = filtered.Where(r => r.Rule.CategoryId == SelectedCategoryFilter.Id);

        Rules.Clear();
        foreach (var item in filtered)
            Rules.Add(item);
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in _allItems) item.IsSelected = true;
        RaiseSelectionChanged();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in _allItems) item.IsSelected = false;
        RaiseSelectionChanged();
    }

    [RelayCommand]
    public async Task ExportSelectedAsync()
    {
        var selected = _allItems.Where(r => r.IsSelected)
            .Select(r => (r.Rule, r.CategoryName))
            .ToList();
        if (selected.Count == 0) return;

        IsExporting = true;
        try
        {
            var json = await _exportService.ExportAsync(selected);
            var fileName = $"pesascope-rules-{DateTime.Now:yyyyMMdd-HHmmss}.json";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);

            if (!result.IsSuccessful)
            {
                // User cancelled the picker — not an error, just don't show anything
                if (result.Exception is not null)
                    await Shell.Current.DisplayAlertAsync("Export Failed", result.Exception.Message, "OK");
            }
        }
        finally
        {
            IsExporting = false;
        }
    }
}

/// <summary>
/// Wraps a rule with UI selection state for the export list.
/// </summary>
public partial class RuleSelectionItem : ObservableObject
{
    public required AutoCategorizationRule Rule { get; init; }
    public required string CategoryName { get; init; }

    [ObservableProperty] private bool _isSelected;
}