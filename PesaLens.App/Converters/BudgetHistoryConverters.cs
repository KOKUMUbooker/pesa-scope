using System.Globalization;

namespace PesaLens.App.Converters;

/// <summary>
/// Returns true only when ALL bound bool values are true.
/// Use with MultiBinding to gate visibility on multiple conditions.
/// </summary>
public class AllTrueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.All(v => v is true);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}