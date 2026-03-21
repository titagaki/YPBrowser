using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

public partial class BehaviorPageViewModel : ObservableObject
{
    private readonly BehaviorSettings _settings;

    [ObservableProperty] private string _refreshIntervalSeconds;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _notifyOnFavorite;
    [ObservableProperty] private bool _openOnDoubleClick;

    public BehaviorPageViewModel(BehaviorSettings settings)
    {
        _settings = settings;
        _refreshIntervalSeconds = settings.RefreshIntervalSeconds.ToString();
        _minimizeToTray = settings.MinimizeToTray;
        _startMinimized = settings.StartMinimized;
        _notifyOnFavorite = settings.NotifyOnFavorite;
        _openOnDoubleClick = settings.OpenOnDoubleClick;
    }

    partial void OnRefreshIntervalSecondsChanged(string value)
    {
        if (int.TryParse(value, out var v))
            _settings.RefreshIntervalSeconds = v;
    }

    partial void OnMinimizeToTrayChanged(bool value) => _settings.MinimizeToTray = value;
    partial void OnStartMinimizedChanged(bool value) => _settings.StartMinimized = value;
    partial void OnNotifyOnFavoriteChanged(bool value) => _settings.NotifyOnFavorite = value;
    partial void OnOpenOnDoubleClickChanged(bool value) => _settings.OpenOnDoubleClick = value;
}
