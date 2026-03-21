using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Helpers;
using YPBrowser.Models;
using YPBrowser.Services;

namespace YPBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAutoRefreshService _refreshService;
    private readonly IChannelDiffService _diffService;
    private readonly IFavoriteMatchService _favoriteService;
    private readonly INotificationService _notificationService;
    private readonly IPlayerLaunchService _playerService;
    private readonly ISettingsService _settings;
    private readonly IChannelFilterService _filterService;
    private readonly IRecordService _recordService;
    private readonly IAutoDownloadMatchService _autoDownloadService;
    private Dispatcher? _dispatcher;

    // Full merged channel list (all YPs, including log)
    private readonly List<ChannelItem> _allChannels = [];

    public ObservableCollection<ChannelItem> FilteredChannels { get; } = [];

    [ObservableProperty] private ChannelItem? _selectedChannel;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private int _activeFilterIndex = 0;
    [ObservableProperty] private bool _isRefreshing = false;
    [ObservableProperty] private string _statusText = "準備完了";
    [ObservableProperty] private string _nextRefreshText = "";

    public string[] FilterLabels { get; } = ["すべて", "新着", "お気に入り", "NG", "ログ"];

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnActiveFilterIndexChanged(int value) => ApplyFilter();

    public MainViewModel(
        IAutoRefreshService refreshService,
        IChannelDiffService diffService,
        IFavoriteMatchService favoriteService,
        INotificationService notificationService,
        IPlayerLaunchService playerService,
        ISettingsService settings,
        IChannelFilterService filterService,
        IRecordService recordService,
        IAutoDownloadMatchService autoDownloadService)
    {
        _refreshService = refreshService;
        _diffService = diffService;
        _favoriteService = favoriteService;
        _notificationService = notificationService;
        _playerService = playerService;
        _settings = settings;
        _filterService = filterService;
        _recordService = recordService;
        _autoDownloadService = autoDownloadService;

        _refreshService.RefreshStarted += OnRefreshStarted;
        _refreshService.RefreshCompleted += OnRefreshCompleted;
    }

    public void Initialize(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _refreshService.Start();

        // Countdown timer
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            var remaining = _refreshService.NextRefreshAt - DateTime.Now;
            NextRefreshText = remaining > TimeSpan.Zero
                ? $"次回更新: {(int)remaining.TotalSeconds}秒後"
                : "更新中...";
        };
        timer.Start();
    }

    private void OnRefreshStarted(object? sender, EventArgs e)
    {
        _dispatcher?.BeginInvoke(() => IsRefreshing = true);
    }

    private void OnRefreshCompleted(object? sender, RefreshCompletedEventArgs e)
    {
        _dispatcher?.BeginInvoke(() =>
        {
            var newList = e.Channels.ToList();
            var favorites = FavoriteSettingsMapper.ToFavoriteItems(_settings.Current.Favorites);

            bool isFirstFetch = _allChannels.Count == 0;

            _diffService.ApplyDiff(_allChannels, newList);
            _favoriteService.MatchAll(newList, favorites);

            var newFavs = _favoriteService.GetNewFavoriteChannels(newList);
            if (_settings.Current.Behavior.NotifyOnFavorite && newFavs.Count > 0)
                _notificationService.NotifyNewFavorites(newFavs);

            if (!isFirstFetch && !string.IsNullOrWhiteSpace(_settings.Current.Downloader.ExecutablePath))
            {
                var rules = Helpers.AutoDownloadSettingsMapper.ToRuleItems(_settings.Current.AutoDownloadRules);
                if (rules.Count > 0)
                {
                    foreach (var ch in _autoDownloadService.GetChannelsToAutoDownload(newList, rules))
                        _recordService.Record(ch, _settings.Current.Downloader);
                }
            }

            _allChannels.Clear();
            _allChannels.AddRange(newList);
            _allChannels.AddRange(_diffService.GetAllLogChannels());

            ApplyFilter();
            UpdateStatus();

            IsRefreshing = false;
        });
    }

    private void ApplyFilter()
    {
        var filtered = _filterService.Filter(_allChannels, ActiveFilterIndex, SearchText);
        FilteredChannels.Clear();
        foreach (var ch in filtered)
            FilteredChannels.Add(ch);
    }

    private void UpdateStatus()
    {
        var total = _allChannels.Count(c => c.Diff != ChannelDiff.Log);
        var listeners = _allChannels
            .Where(c => c.Diff != ChannelDiff.Log && c.Listeners > 0)
            .Sum(c => c.Listeners);
        StatusText = $"{total}チャンネル / {listeners}人視聴中";
        _notificationService.UpdateTrayTooltip(total, listeners);
    }

    [RelayCommand]
    private async Task Refresh() => await _refreshService.RefreshNowAsync();

    [RelayCommand]
    private void OpenChannel(ChannelItem? channel)
    {
        if (channel == null) return;
        var players = _settings.Current.Players;
        var defaultPlayer = players.FirstOrDefault(p => p.IsDefault) ?? players.FirstOrDefault();
        if (defaultPlayer == null)
        {
            _playerService.LaunchWithDefault(channel);
            return;
        }
        var playerModel = new PlayerItem
        {
            Name = defaultPlayer.Name,
            ExecutablePath = defaultPlayer.ExecutablePath,
            ArgumentTemplate = defaultPlayer.ArgumentTemplate,
            UsePlaylistFile = defaultPlayer.UsePlaylistFile,
        };
        _playerService.Launch(channel, playerModel);
    }

    [RelayCommand]
    private void OpenChannelWith(OpenChannelWithArgs args)
    {
        if (args.Channel == null || args.Player == null) return;
        _playerService.Launch(args.Channel, args.Player);
    }

    [RelayCommand]
    private void CopyUrl(ChannelItem? channel)
    {
        if (channel == null) return;
        Clipboard.SetText(channel.StreamUrl);
    }

    [RelayCommand]
    private void CopyChannelDetail(ChannelItem? channel)
    {
        if (channel == null) return;

        var comment = string.IsNullOrWhiteSpace(channel.Comment)
            ? ""
            : $" 「{channel.Comment}」";

        var genreDesc = string.Join(" - ",
            new[] { channel.Genre, channel.Description }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var trackParts = new[] { channel.TrackArtist, channel.TrackAlbum, channel.TrackTitle }
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{channel.ChannelName} ({channel.ListenersDisplay}/ {channel.RelaysDisplay})");
        sb.AppendLine($" [{genreDesc}]{comment}");
        if (trackParts.Count > 0)
            sb.AppendLine($" {string.Join(" / ", trackParts)}");
        sb.Append(channel.ContactUrl);

        Clipboard.SetText(sb.ToString());
    }

    [RelayCommand]
    private async Task AddFavorite(ChannelItem? channel)
    {
        if (channel == null) return;
        _settings.Current.Favorites.Add(new Settings.FavoriteSettings
        {
            Title = channel.ChannelName,
            Word = channel.ChannelName,
            TargetFields = ["ChannelName"],
            BackColor = "#FFFF99",
            TextColor = "#000000",
        });
        await _settings.SaveAsync();
        var favorites = Helpers.FavoriteSettingsMapper.ToFavoriteItems(_settings.Current.Favorites);
        _favoriteService.MatchAll(_allChannels, favorites);
        ApplyFilter();
    }

    [RelayCommand]
    private void StartRecord(ChannelItem? channel)
    {
        if (channel == null) return;
        var dl = _settings.Current.Downloader;
        if (string.IsNullOrWhiteSpace(dl.ExecutablePath))
        {
            StatusText = "録音ツールが設定されていません。設定 > ダウンロード を確認してください。";
            return;
        }
        _recordService.Record(channel, dl);
    }

    [RelayCommand]
    private async Task RemoveFavorite(ChannelItem? channel)
    {
        if (channel == null) return;
        var toRemove = _settings.Current.Favorites
            .Where(f => !f.IsRegex && f.Word == channel.ChannelName)
            .ToList();
        foreach (var f in toRemove)
            _settings.Current.Favorites.Remove(f);
        await _settings.SaveAsync();
        var favorites = Helpers.FavoriteSettingsMapper.ToFavoriteItems(_settings.Current.Favorites);
        _favoriteService.MatchAll(_allChannels, favorites);
        ApplyFilter();
    }
}

public record OpenChannelWithArgs(ChannelItem? Channel, PlayerItem? Player);
