using PesaScope.App.ViewModels;

namespace PesaScope.App.Views.Settings;

public partial class RuleExportPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly RuleExportViewModel _vm;
    private bool _loaded;

    public RuleExportPage(RuleExportViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        await _vm.LoadAsync();
    }
}