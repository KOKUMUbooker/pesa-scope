using System.Globalization;
using PesaLens.App.Models;

namespace PesaLens.App.Converters;

/// <summary>
/// Returns true when a string is non-null and non-empty.
/// Used to show/hide the search clear (✕) button.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Maps a TransactionType to an emoji icon for the list row icon circle.
/// </summary>
public class TransactionTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TransactionType type ? GetIcon(type) : "💳";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static string GetIcon(TransactionType type) => type switch
    {
        TransactionType.SendMoney => "📤",
        TransactionType.ReceiveMoney => "📥",
        TransactionType.PayBill => "🧾",
        TransactionType.BuyGoods => "🛒",
        TransactionType.AirtimePurchase => "📱",
        TransactionType.Withdrawal => "🏧",
        TransactionType.Deposit => "💰",
        TransactionType.Fuliza => "⚡",
        TransactionType.MShwari => "🏦",
        TransactionType.Reversal => "↩️",
        _ => "💳"
    };
}

/// <summary>
/// Maps a TransactionType to a Color — green for incoming, red for outgoing.
/// </summary>
public class TransactionPageTypeToColorConverter : IValueConverter
{
    private static readonly Color Incoming = Color.FromArgb("#1A8C62");
    private static readonly Color Outgoing = Color.FromArgb("#C0392B");

    private static readonly HashSet<TransactionType> OutgoingTypes =
    [
        TransactionType.SendMoney,
        TransactionType.PayBill,
        TransactionType.BuyGoods,
        TransactionType.AirtimePurchase,
        TransactionType.Withdrawal,
        TransactionType.Fuliza,
        TransactionType.MShwari
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TransactionType type && OutgoingTypes.Contains(type) ? Outgoing : Incoming;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}