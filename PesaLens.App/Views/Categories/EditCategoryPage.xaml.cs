using PesaLens.App.ViewModels.Categories;

namespace PesaLens.App.Views.Categories;

public partial class EditCategoryPage : UraniumUI.Pages.UraniumContentPage
{
    public EditCategoryPage(EditCategoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}