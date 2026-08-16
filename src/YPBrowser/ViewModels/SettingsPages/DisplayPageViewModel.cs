using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

public partial class DisplayPageViewModel : ObservableObject
{
    private readonly DisplaySettings _settings;

    [ObservableProperty] private string _newChannelColor;

    public DisplayPageViewModel(DisplaySettings settings)
    {
        _settings = settings;
        _newChannelColor = settings.NewChannelColor;
    }

    partial void OnNewChannelColorChanged(string value) => _settings.NewChannelColor = value;
}
