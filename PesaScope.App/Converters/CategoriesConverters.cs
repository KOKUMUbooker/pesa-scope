using System.Globalization;

namespace PesaLens.App.Converters;

// <summary>
// Compares the bound integer value against <see cref="CompareValue"/> and
// returns <see cref="TrueValue"/> when equal, <see cref="FalseValue"/> otherwise.
//
// Used by the Categories tab bar to colour the active tab label and indicator:
//
//   <converters:IntEqualsToBrushConverter
//       TrueValue="{AppThemeBinding ...Primary}"
//       FalseValue="{AppThemeBinding ...OnSurfaceVariant}"
//       CompareValue="0"/>
// </summary>
public class IntEqualsToBrushConverter : IValueConverter
{
    /// <summary>The integer to compare the binding value against.</summary>
    public int CompareValue { get; set; }

    /// <summary>Returned when the binding value equals <see cref="CompareValue"/>.</summary>
    public object? TrueValue { get; set; }

    /// <summary>Returned when the binding value does not equal <see cref="CompareValue"/>.</summary>
    public object? FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return intValue == CompareValue ? TrueValue : FalseValue;

        return FalseValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}


/// <summary>
/// Converts a double (0.0–1.0 percentage ratio) to a pixel width
/// by multiplying against the screen width minus horizontal padding.
/// Used to size the category progress bar fill.
/// </summary>
public class DoubleToWidthConverter : IValueConverter
{
    /// <summary>
    /// Horizontal margin deducted from screen width before calculating
    /// the bar width. Default matches the page's 40px total side padding.
    /// </summary>
    public double HorizontalPadding { get; set; } = 40;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double ratio = value is double d ? d : 0.0;
        ratio = Math.Clamp(ratio, 0.0, 1.0);

        double screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width
                           / DeviceDisplay.Current.MainDisplayInfo.Density;

        // Subtract card padding (32px) + page padding (40px) + icon+spacing (44+12px)
        double availableWidth = screenWidth - HorizontalPadding - 32 - 56;

        return Math.Max(0, availableWidth * ratio);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

public class NullToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter as string)?.Split('|');
        if (parts is not { Length: 2 })
            return value;

        return value is null ? parts[1] : parts[0];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try { return Color.FromArgb(hex); }
            catch { }
        }
        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToRotationConverter : IValueConverter
{
    // Chevron points right when collapsed (0°), rotates down when expanded (90°).
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool expanded && expanded ? 90d : 0d;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
