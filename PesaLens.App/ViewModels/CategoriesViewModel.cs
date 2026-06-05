using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaLens.App.Data.Repositories.Interfaces;
using PesaLens.App.Models;
using PesaLens.App.Views.Categories;

namespace PesaLens.App.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IAutoCategorizationRuleRepository _ruleRepo;

    [ObservableProperty] private List<CategoryWithRules> _systemCategories = [];
    [ObservableProperty] private List<CategoryWithRules> _customCategories = [];
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _hasCustomCategories;

    public CategoriesViewModel(
        ICategoryRepository categoryRepo,
        IAutoCategorizationRuleRepository ruleRepo)
    {
        _categoryRepo = categoryRepo;
        _ruleRepo = ruleRepo;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;

        var categories = await _categoryRepo.GetAllActiveAsync();
        var allRules = await _ruleRepo.GetEnabledOrderedByPriorityAsync();

        // Build enriched list with rule counts
        var enriched = categories.Select(cat => new CategoryWithRules
        {
            Category = cat,
            RuleCount = allRules.Count(r => r.CategoryId == cat.Id)
        }).ToList();

        SystemCategories = enriched.Where(c => c.Category.IsSystemCategory).ToList();
        CustomCategories = enriched.Where(c => !c.Category.IsSystemCategory).ToList();
        HasCustomCategories = CustomCategories.Count > 0;

        IsLoading = false;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task CreateCategoryAsync()
    {
        await Shell.Current.GoToAsync(nameof(EditCategoryPage));
    }

    [RelayCommand]
    public async Task EditCategoryAsync(CategoryWithRules item)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(EditCategoryPage)}?categoryId={item.Category.Id}");
    }

    [RelayCommand]
    public async Task DeleteCategoryAsync(CategoryWithRules item)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            $"Delete \"{item.Category.Name}\"?",
            "All transactions in this category will be moved to Uncategorized. This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        await _categoryRepo.DeleteAndReassignAsync(item.Category.Id);
        await LoadAsync();
    }
}

// ── Helper model ──────────────────────────────────────────────────────────────

public class CategoryWithRules
{
    public Category Category { get; set; } = new();
    public int RuleCount { get; set; }

    public string RuleCountText => RuleCount == 0
        ? "No active rules"
        : $"{RuleCount} auto-rule{(RuleCount == 1 ? "" : "s")}";

    public bool HasRules => RuleCount > 0;
}