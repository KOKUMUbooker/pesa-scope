using PesaLens.App.ViewModels;

namespace PesaLens.App.Controls.Home;

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

    public TopCategoriesView()
    {
        InitializeComponent();
    }
}