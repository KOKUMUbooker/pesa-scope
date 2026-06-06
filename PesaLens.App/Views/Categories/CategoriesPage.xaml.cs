using PesaLens.App.ViewModels;

namespace PesaLens.App.Views.Categories;

public partial class CategoriesPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly CategoriesViewModel _vm;

    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}