using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

public partial class NetworkPageViewModel : ObservableObject
{
    private readonly NetworkSettings _settings;

    [ObservableProperty] private string _proxyUrl;
    [ObservableProperty] private string _userAgent;
    [ObservableProperty] private string _timeoutSeconds;

    public NetworkPageViewModel(NetworkSettings settings)
    {
        _settings = settings;
        _proxyUrl = settings.ProxyUrl;
        _userAgent = settings.UserAgent;
        _timeoutSeconds = settings.TimeoutSeconds.ToString();
    }

    partial void OnProxyUrlChanged(string value) => _settings.ProxyUrl = value;
    partial void OnUserAgentChanged(string value) => _settings.UserAgent = value;

    partial void OnTimeoutSecondsChanged(string value)
    {
        if (int.TryParse(value, out var v))
            _settings.TimeoutSeconds = v;
    }
}
