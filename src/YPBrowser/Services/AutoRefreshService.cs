using YPBrowser.Abstractions;
using YPBrowser.Helpers;
using YPBrowser.Models;

namespace YPBrowser.Services;

public class AutoRefreshService : IAutoRefreshService, IDisposable
{
    /// <summary>「更新しない」のときに設定の変更へ気付くための見張り間隔。</summary>
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(5);

    private readonly IYpFetchService _fetchService;
    private readonly ISettingsService _settings;
    private CancellationTokenSource _cts = new();
    private Task? _timerTask;
    private bool _disposed;

    public event EventHandler<RefreshCompletedEventArgs>? RefreshCompleted;
    public event EventHandler? RefreshStarted;

    public bool IsRefreshing { get; private set; }
    public DateTime NextRefreshAt { get; private set; } = DateTime.Now;

    public AutoRefreshService(IYpFetchService fetchService, ISettingsService settings)
    {
        _fetchService = fetchService;
        _settings = settings;
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _timerTask = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
        _timerTask = null;
    }

    public Task RefreshNowAsync() => DoRefreshAsync(CancellationToken.None);

    private async Task RunLoopAsync(CancellationToken ct)
    {
        // Immediate first fetch
        await DoRefreshAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            var configured = _settings.Current.Behavior.RefreshIntervalSeconds;

            // 0 =「更新しない」。設定は閉じずに変えられるので、ループ自体は止めずに見張り続ける
            if (configured <= 0)
            {
                NextRefreshAt = DateTime.MaxValue;
                try
                {
                    await Task.Delay(IdlePollInterval, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                continue;
            }

            var intervalSec = Math.Max(SettingsMigration.MinRefreshIntervalSeconds, configured);
            NextRefreshAt = DateTime.Now.AddSeconds(intervalSec);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSec), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            await DoRefreshAsync(ct);
        }
    }

    /// <summary>
    /// 有効な YP を設定順に取得する。取得側では間引かない
    /// （実アクセス間隔 = ユーザーが設定した更新間隔。負荷対策はプリセットの下限で行う）。
    /// </summary>
    private async Task DoRefreshAsync(CancellationToken ct)
    {
        if (IsRefreshing) return;
        IsRefreshing = true;
        RefreshStarted?.Invoke(this, EventArgs.Empty);

        var allChannels = new List<ChannelItem>();
        var servers = _settings.Current.YpServers;
        var network = _settings.Current.Network;

        foreach (var serverSettings in servers.Where(s => s.Enabled))
        {
            var server = new YpServerItem
            {
                Name = serverSettings.Name,
                Url = serverSettings.Url,
                Host = serverSettings.Host,
                Enabled = serverSettings.Enabled,
                BitrateMin = serverSettings.BitrateMin,
                BitrateMax = serverSettings.BitrateMax,
                TypeFilter = serverSettings.TypeFilter,
            };

            var channels = await _fetchService.FetchAsync(server, network, ct);
            allChannels.AddRange(channels);
        }

        IsRefreshing = false;
        RefreshCompleted?.Invoke(this, new RefreshCompletedEventArgs(allChannels));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _cts.Dispose();
            _disposed = true;
        }
    }
}

public class RefreshCompletedEventArgs(List<ChannelItem> channels) : EventArgs
{
    public IReadOnlyList<ChannelItem> Channels { get; } = channels;
}
