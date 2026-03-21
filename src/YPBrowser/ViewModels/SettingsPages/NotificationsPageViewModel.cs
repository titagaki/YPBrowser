using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

public partial class NotificationsPageViewModel : ObservableObject
{
    private readonly NotificationSettings _settings;

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private string _balloonTimeoutSeconds;

    public NotificationsPageViewModel(NotificationSettings settings)
    {
        _settings = settings;
        _enabled = settings.Enabled;
        _balloonTimeoutSeconds = settings.BalloonTimeoutSeconds.ToString();
    }

    partial void OnEnabledChanged(bool value) => _settings.Enabled = value;

    partial void OnBalloonTimeoutSecondsChanged(string value)
    {
        if (int.TryParse(value, out var v))
            _settings.BalloonTimeoutSeconds = v;
    }
}
