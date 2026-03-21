using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Models;
using YPBrowser.Services;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AutoRefreshService _refreshService;
    private readonly ChannelDiffService _diffService;
    private readonly FavoriteMatchService _favoriteService;
    private readonly NotificationService _notificationService;
    private readonly PlayerLaunchService _playerService;
    private readonly SettingsService _settings;
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
        AutoRefreshService refreshService,
        ChannelDiffService diffService,
        FavoriteMatchService favoriteService,
        NotificationService notificationService,
        PlayerLaunchService playerService,
        SettingsService settings)
    {
        _refreshService = refreshService;
        _diffService = diffService;
        _favoriteService = favoriteService;
        _notificationService = notificationService;
        _playerService = playerService;
        _settings = settings;

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
            var favorites = GetFavoriteItems();

            // Apply diff
            _diffService.ApplyDiff(_allChannels, newList);

            // Apply favorite matching
            _favoriteService.MatchAll(newList, favorites);

            // Notify new favorites
            var newFavs = _favoriteService.GetNewFavoriteChannels(newList);
            if (_settings.Current.Behavior.NotifyOnFavorite && newFavs.Count > 0)
                _notificationService.NotifyNewFavorites(newFavs);

            // Update full list
            _allChannels.Clear();
            _allChannels.AddRange(newList);
            // Add log channels
            _allChannels.AddRange(_diffService.GetAllLogChannels());

            ApplyFilter();
            UpdateStatus();

            IsRefreshing = false;
        });
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        IEnumerable<ChannelItem> source = _allChannels;

        // Tab filter
        source = ActiveFilterIndex switch
        {
            1 => source.Where(c => c.Diff == ChannelDiff.New),
            2 => source.Where(c => c.IsFavorite && !c.IsNG),
            3 => source.Where(c => c.IsNG),
            4 => source.Where(c => c.Diff == ChannelDiff.Log),
            _ => source.Where(c => c.Diff != ChannelDiff.Log && !c.IsNG),
        };

        // Search text filter
        if (!string.IsNullOrEmpty(query))
        {
            source = source.Where(c =>
                c.ChannelName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Genre.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Comment.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        // Sort: favorites first, then by listeners desc
        var sorted = source
            .OrderByDescending(c => c.IsFavorite ? 1 : 0)
            .ThenByDescending(c => c.Listeners);

        FilteredChannels.Clear();
        foreach (var ch in sorted)
            FilteredChannels.Add(ch);
    }

    private void UpdateStatus()
    {
        var total = _allChannels.Count(c => c.Diff != ChannelDiff.Log);
        var listeners = _allChannels.Where(c => c.Diff != ChannelDiff.Log && c.Listeners > 0)
                                    .Sum(c => c.Listeners);
        StatusText = $"{total}チャンネル / {listeners}人視聴中";
        _notificationService.UpdateTrayTooltip(total, listeners);
    }

    private List<FavoriteItem> GetFavoriteItems()
    {
        return _settings.Current.Favorites.Select(f => new FavoriteItem
        {
            Title = f.Title,
            Word = f.Word,
            TargetFields = ParseTargetFields(f.TargetFields),
            IsRegex = f.IsRegex,
            IsNG = f.IsNG,
            NotifyEnabled = f.NotifyEnabled,
            Enabled = f.Enabled,
            BackColor = f.BackColor,
            TextColor = f.TextColor,
            SoundFile = f.SoundFile,
        }).ToList();
    }

    private static FavoriteTargetFields ParseTargetFields(List<string> fields)
    {
        var result = FavoriteTargetFields.None;
        foreach (var f in fields)
        {
            if (Enum.TryParse<FavoriteTargetFields>(f, out var flag))
                result |= flag;
        }
        return result == FavoriteTargetFields.None ? FavoriteTargetFields.ChannelName : result;
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
}

public record OpenChannelWithArgs(ChannelItem? Channel, PlayerItem? Player);
