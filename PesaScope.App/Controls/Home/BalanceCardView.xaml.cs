using System.Windows.Input;

namespace PesaScope.App.Controls.Home;

public partial class BalanceCardView : ContentView
{
    public static readonly BindableProperty NetBalanceProperty =
        BindableProperty.Create(nameof(NetBalance), typeof(string), typeof(BalanceCardView), "KSh 0");

    public static readonly BindableProperty PeriodLabelProperty =
        BindableProperty.Create(nameof(PeriodLabel), typeof(string), typeof(BalanceCardView), string.Empty);

    public static readonly BindableProperty MoneyInProperty =
        BindableProperty.Create(nameof(MoneyIn), typeof(string), typeof(BalanceCardView), "KSh 0");

    public static readonly BindableProperty MoneyOutProperty =
        BindableProperty.Create(nameof(MoneyOut), typeof(string), typeof(BalanceCardView), "KSh 0");

    public static readonly BindableProperty ShowHelpCommandProperty =
        BindableProperty.Create(nameof(ShowHelpCommand), typeof(ICommand), typeof(BalanceCardView));

    public static readonly BindableProperty ShowMonthSummaryProperty =
    BindableProperty.Create(nameof(ShowMonthSummary), typeof(bool), typeof(BalanceCardView), false);

    public static readonly BindableProperty MonthLabelProperty =
        BindableProperty.Create(nameof(MonthLabel), typeof(string), typeof(BalanceCardView), string.Empty);

    public static readonly BindableProperty MonthMoneyInProperty =
        BindableProperty.Create(nameof(MonthMoneyIn), typeof(string), typeof(BalanceCardView), "Ksh 0");

    public static readonly BindableProperty MonthMoneyOutProperty =
        BindableProperty.Create(nameof(MonthMoneyOut), typeof(string), typeof(BalanceCardView), "Ksh 0");

    public static readonly BindableProperty IsMonthSummaryExpandedProperty =
        BindableProperty.Create(nameof(IsMonthSummaryExpanded), typeof(bool), typeof(BalanceCardView), false,
            propertyChanged: (b, _, n) => ((BalanceCardView)b).AnimateChevron((bool)n));

    public string NetBalance
    {
        get => (string)GetValue(NetBalanceProperty);
        set => SetValue(NetBalanceProperty, value);
    }

    public string PeriodLabel
    {
        get => (string)GetValue(PeriodLabelProperty);
        set => SetValue(PeriodLabelProperty, value);
    }

    public string MoneyIn
    {
        get => (string)GetValue(MoneyInProperty);
        set => SetValue(MoneyInProperty, value);
    }

    public string MoneyOut
    {
        get => (string)GetValue(MoneyOutProperty);
        set => SetValue(MoneyOutProperty, value);
    }

    public ICommand ShowHelpCommand
    {
        get => (ICommand)GetValue(ShowHelpCommandProperty);
        set => SetValue(ShowHelpCommandProperty, value);
    }


    public bool ShowMonthSummary
    {
        get => (bool)GetValue(ShowMonthSummaryProperty);
        set => SetValue(ShowMonthSummaryProperty, value);
    }

    public string MonthLabel
    {
        get => (string)GetValue(MonthLabelProperty);
        set => SetValue(MonthLabelProperty, value);
    }

    public string MonthMoneyIn
    {
        get => (string)GetValue(MonthMoneyInProperty);
        set => SetValue(MonthMoneyInProperty, value);
    }

    public string MonthMoneyOut
    {
        get => (string)GetValue(MonthMoneyOutProperty);
        set => SetValue(MonthMoneyOutProperty, value);
    }

    public bool IsMonthSummaryExpanded
    {
        get => (bool)GetValue(IsMonthSummaryExpandedProperty);
        set => SetValue(IsMonthSummaryExpandedProperty, value);
    }

    private void OnMonthSummaryTapped(object? sender, TappedEventArgs e) =>
        IsMonthSummaryExpanded = !IsMonthSummaryExpanded;

    private void AnimateChevron(bool expanded) =>
        _ = MonthChevron.RotateTo(expanded ? 180 : 0, 200, Easing.CubicOut);

    public BalanceCardView()
    {
        InitializeComponent();
    }
}