using PesaScope.App.ViewModels;

namespace PesaScope.App.Views.Settings;

public partial class ExportPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly ExportViewModel _vm;

    public ExportPage(ExportViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Always reload — recent exports and available budget-snapshot years
        // may have changed since the last visit
        await _vm.LoadAsync();
    }
}