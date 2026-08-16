using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

public partial class NotificationsPageViewModel : ObservableObject
{
    private readonly NotificationSettings _settings;

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private bool _notifyOnFavorite;
    [ObservableProperty] private string _balloonTimeoutSeconds;

    public NotificationsPageViewModel(NotificationSettings settings)
    {
        _settings = settings;
        _enabled = settings.Enabled;
        _notifyOnFavorite = settings.NotifyOnFavorite;
        _balloonTimeoutSeconds = settings.BalloonTimeoutSeconds.ToString();
    }

    partial void OnEnabledChanged(bool value) => _settings.Enabled = value;
    partial void OnNotifyOnFavoriteChanged(bool value) => _settings.NotifyOnFavorite = value;

    partial void OnBalloonTimeoutSecondsChanged(string value)
    {
        if (int.TryParse(value, out var v))
            _settings.BalloonTimeoutSeconds = v;
    }
}
