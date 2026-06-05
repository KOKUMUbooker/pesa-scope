using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Categories;

public partial class CategoriesPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly CategoriesViewModel _vm;

    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reload on every appearance so edits/creates made on EditCategoryPage
        // are reflected immediately when the user navigates back
        await _vm.LoadAsync();
    }
}