using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace YPBrowser.Converters;

public class NullableColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Windows.Media.Color color)
            return new SolidColorBrush(color);
        if (parameter is string hexDefault && !string.IsNullOrEmpty(hexDefault))
            return ParseHexBrush(hexDefault);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static SolidColorBrush? ParseHexBrush(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            var r = System.Convert.ToByte(hex[0..2], 16);
            var g = System.Convert.ToByte(hex[2..4], 16);
            var b = System.Convert.ToByte(hex[4..6], 16);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, r, g, b));
        }
        catch { return null; }
    }
}
