using PesaScope.App.ViewModels;
using System.Windows.Input;

namespace PesaScope.App.Controls.Home;

public partial class TopCategoriesView : ContentView
{
    public static readonly BindableProperty CategoriesProperty =
        BindableProperty.Create(
            nameof(Categories),
            typeof(IList<CategorySpendItem>),
            typeof(TopCategoriesView),
            defaultValue: new List<CategorySpendItem>());

    public IList<CategorySpendItem> Categories
    {
        get => (IList<CategorySpendItem>)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public static readonly BindableProperty OpenCategoryCommandProperty =
        BindableProperty.Create(
            nameof(OpenCategoryCommand),
            typeof(ICommand),
            typeof(TopCategoriesView));

    public ICommand OpenCategoryCommand
    {
        get => (ICommand)GetValue(OpenCategoryCommandProperty);
        set => SetValue(OpenCategoryCommandProperty, value);
    }

    public TopCategoriesView()
    {
        InitializeComponent();
    }
}