using PesaScope.App.ViewModels;

namespace PesaScope.App.Views.Settings;

public partial class RuleImportPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly RuleImportViewModel _vm;

    public RuleImportPage(RuleImportViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.RequestClose += async () => await Shell.Current.GoToAsync("..");
    }

    //protected override void OnAppearing()
    //{
    //    base.OnAppearing();
    //    _vm.Load();
    //}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _vm.Load();
        }
        catch (Exception)
        {
            _ = Shell.Current.DisplayAlertAsync("Import Error", "Something went wrong reading the import file. Please try again.", "OK");
        }
    }
}