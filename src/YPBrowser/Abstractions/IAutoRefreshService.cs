using YPBrowser.Services;

namespace YPBrowser.Abstractions;

public interface IAutoRefreshService
{
    event EventHandler<RefreshCompletedEventArgs>? RefreshCompleted;
    event EventHandler? RefreshStarted;
    bool IsRefreshing { get; }
    DateTime NextRefreshAt { get; }
    void Start();
    void Stop();
    Task RefreshNowAsync();
}
