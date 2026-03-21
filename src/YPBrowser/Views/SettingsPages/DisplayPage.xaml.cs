using Microsoft.UI.Xaml.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public sealed partial class DisplayPage : Page
{
    private readonly SettingsService _settings;

    public DisplayPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        var d = _settings.Current.Display;
        FontFamilyBox.Text = d.FontFamily;
        FontSizeBox.Value = d.FontSize;
        FavColorBox.Text = d.FavoriteNameColor;
        NewColorBox.Text = d.NewChannelColor;
    }

    private void FontFamilyBox_TextChanged(object sender, TextChangedEventArgs e)
        => _settings.Current.Display.FontFamily = FontFamilyBox.Text;

    private void FontSizeBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        => _settings.Current.Display.FontSize = args.NewValue;

    private void FavColorBox_TextChanged(object sender, TextChangedEventArgs e)
        => _settings.Current.Display.FavoriteNameColor = FavColorBox.Text;

    private void NewColorBox_TextChanged(object sender, TextChangedEventArgs e)
        => _settings.Current.Display.NewChannelColor = NewColorBox.Text;
}
