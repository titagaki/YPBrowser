using System.Globalization;
using System.Windows.Data;

namespace YPBrowser.Converters;

/// <summary>null なら false。選択が無いときに編集フォームを無効化するのに使う。</summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
