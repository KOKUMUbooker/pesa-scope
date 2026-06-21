using System.Globalization;
using UraniumUI.Icons.MaterialSymbols;

namespace PesaLens.App.Converters;

public class IconNameToGlyphConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string fieldName) return "?";

        var field = typeof(MaterialSharp).GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        return field?.GetValue(null) as string ?? "?";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}