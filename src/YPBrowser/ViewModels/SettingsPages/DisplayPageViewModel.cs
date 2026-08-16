using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

public partial class DisplayPageViewModel : ObservableObject
{
    private readonly DisplaySettings _settings;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string _fontSize;
    [ObservableProperty] private string _newChannelColor;

    public DisplayPageViewModel(DisplaySettings settings)
    {
        _settings = settings;
        _fontFamily = settings.FontFamily;
        _fontSize = settings.FontSize.ToString();
        _newChannelColor = settings.NewChannelColor;
    }

    partial void OnFontFamilyChanged(string value) => _settings.FontFamily = value;

    partial void OnFontSizeChanged(string value)
    {
        if (double.TryParse(value, out var v))
            _settings.FontSize = v;
    }

    partial void OnNewChannelColorChanged(string value) => _settings.NewChannelColor = value;
}
