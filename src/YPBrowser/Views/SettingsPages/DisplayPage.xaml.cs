using System.Windows;
using System.Windows.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public partial class DisplayPage : UserControl
{
    private readonly SettingsService _settings;
    private bool _loading;

    public DisplayPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        _loading = true;
        var d = _settings.Current.Display;
        FontFamilyBox.Text = d.FontFamily;
        FontSizeBox.Text = d.FontSize.ToString();
        FavColorBox.Text = d.FavoriteNameColor;
        NewColorBox.Text = d.NewChannelColor;
        _loading = false;
    }

    private void FontFamilyBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) _settings.Current.Display.FontFamily = FontFamilyBox.Text;
    }

    private void FontSizeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && double.TryParse(FontSizeBox.Text, out var v))
            _settings.Current.Display.FontSize = v;
    }

    private void FavColorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) _settings.Current.Display.FavoriteNameColor = FavColorBox.Text;
    }

    private void NewColorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) _settings.Current.Display.NewChannelColor = NewColorBox.Text;
    }
}
