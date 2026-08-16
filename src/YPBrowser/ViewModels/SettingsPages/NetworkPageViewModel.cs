using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

public partial class NetworkPageViewModel : ObservableObject
{
    private readonly NetworkSettings _settings;

    [ObservableProperty] private string _timeoutSeconds;

    public NetworkPageViewModel(NetworkSettings settings)
    {
        _settings = settings;
        _timeoutSeconds = settings.TimeoutSeconds.ToString();
    }

    partial void OnTimeoutSecondsChanged(string value)
    {
        if (int.TryParse(value, out var v))
            _settings.TimeoutSeconds = v;
    }
}
