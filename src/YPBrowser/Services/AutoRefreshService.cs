using Microsoft.Extensions.Logging;
using YPBrowser.Models;

namespace YPBrowser.Services;

public class AutoRefreshService : IDisposable
{
    private static readonly TimeSpan MinFetchInterval = TimeSpan.FromMinutes(4);

    private readonly YpFetchService _fetchService;
    private readonly SettingsService _settings;
    private readonly ILogger<AutoRefreshService> _logger;
    private CancellationTokenSource _cts = new();
    private Task? _timerTask;
    private bool _disposed;

    public event EventHandler<RefreshCompletedEventArgs>? RefreshCompleted;
    public event EventHandler? RefreshStarted;

    public bool IsRefreshing { get; private set; }
    public DateTime NextRefreshAt { get; private set; } = DateTime.Now;

    public AutoRefreshService(
        YpFetchService fetchService,
        SettingsService settings,
        ILogger<AutoRefreshService> logger)
    {
        _fetchService = fetchService;
        _settings = settings;
        _logger = logger;
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
            var intervalSec = Math.Max(30, _settings.Current.Behavior.RefreshIntervalSeconds);
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

            // Minimum interval guard
            if (DateTime.Now - server.LastUpdateTime < MinFetchInterval &&
                server.LastUpdateTime != DateTime.MinValue)
            {
                _logger.LogDebug("Skipping {Name} - too soon since last fetch", server.Name);
                continue;
            }

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
