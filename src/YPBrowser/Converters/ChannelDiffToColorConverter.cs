using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using YPBrowser.Models;

namespace YPBrowser.Converters;

public class ChannelDiffToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ChannelDiff diff)
        {
            return diff switch
            {
                ChannelDiff.New  => new SolidColorBrush(Color.FromArgb(255, 0, 128, 0)),
                ChannelDiff.Up   => new SolidColorBrush(Color.FromArgb(255, 0, 0, 200)),
                ChannelDiff.Down => new SolidColorBrush(Color.FromArgb(255, 200, 0, 0)),
                ChannelDiff.Log  => new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)),
                _                => new SolidColorBrush(Colors.Transparent),
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
