using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Helpers;
using YPBrowser.Models;
using YPBrowser.Services;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAutoRefreshService _refreshService;
    private readonly IChannelDiffService _diffService;
    private readonly ITagMatchService _tagService;
    private readonly INotificationService _notificationService;
    private readonly ITrayIconService _trayService;
    private readonly IPlayerLaunchService _playerService;
    private readonly ISettingsService _settings;
    private readonly IChannelFilterService _filterService;
    private readonly IRecordService _recordService;
    private readonly IAutoDownloadMatchService _autoDownloadService;
    private Dispatcher? _dispatcher;

    // Full merged channel list (all YPs, including log)
    private readonly List<ChannelItem> _allChannels = [];

    public ObservableCollection<ChannelItem> FilteredChannels { get; } = [];
    public ObservableCollection<RecordingEntry> RecordingEntries { get; } = [];

    /// <summary>左のビュー欄。組み込みビューの下にタグが自動で並ぶ。</summary>
    public ObservableCollection<ChannelViewItem> Views { get; } = [];

    [ObservableProperty] private ChannelItem? _selectedChannel;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isRefreshing = false;
    /// <summary>ステータスバー左。録音の操作結果を出すときだけ埋まる。</summary>
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _nextRefreshText = "";

    [ObservableProperty] private ChannelViewItem? _selectedView;

    /// <summary>「一覧から隠す」タグの分を一時的に表示する。</summary>
    [ObservableProperty] private bool _showHidden;

    /// <summary>
    /// 現在のビューで隠されている件数。0 でもバー自体は出し続ける。
    /// フィルタで消えた配信と YP に出ていない配信をユーザーが区別できなくなるため。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HiddenNoticeText))]
    private int _hiddenCount;

    public string HiddenNoticeText => HiddenCount == 0
        ? "非表示なし"
        : $"「一覧から隠す」タグで {HiddenCount} 件を非表示中";

    /// <summary>絞り込み欄の右に出す「N 件表示中 / 全 M 件」。</summary>
    public string ResultCountText => $"{FilteredChannels.Count} 件表示中 / {LiveChannelCount} 件";

    private int LiveChannelCount => _allChannels.Count(c => c.Diff != ChannelDiff.Log);

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedViewChanged(ChannelViewItem? value) => ApplyFilter();
    partial void OnShowHiddenChanged(bool value) => ApplyFilter();

    public MainViewModel(
        IAutoRefreshService refreshService,
        IChannelDiffService diffService,
        ITagMatchService tagService,
        INotificationService notificationService,
        IPlayerLaunchService playerService,
        ISettingsService settings,
        IChannelFilterService filterService,
        IRecordService recordService,
        IAutoDownloadMatchService autoDownloadService,
        ITrayIconService trayService)
    {
        _refreshService = refreshService;
        _diffService = diffService;
        _tagService = tagService;
        _notificationService = notificationService;
        _trayService = trayService;
        _playerService = playerService;
        _settings = settings;
        _filterService = filterService;
        _recordService = recordService;
        _autoDownloadService = autoDownloadService;

        _refreshService.RefreshStarted += OnRefreshStarted;
        _refreshService.RefreshCompleted += OnRefreshCompleted;
        _recordService.RecordingsChanged += OnRecordingsChanged;

        // ビュー欄を「ビュー」「タグ」の2群に分けて出す
        System.Windows.Data.CollectionViewSource.GetDefaultView(Views)
            .GroupDescriptions.Add(
                new System.Windows.Data.PropertyGroupDescription(nameof(ChannelViewItem.GroupName)));
    }

    public void Initialize(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        RebuildViews();
        _refreshService.Start();

        // Countdown + recording elapsed timer
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            var nextAt = _refreshService.NextRefreshAt;
            var remaining = nextAt - DateTime.Now;
            NextRefreshText = nextAt == DateTime.MaxValue
                ? "自動更新: なし"
                : remaining > TimeSpan.Zero
                    ? $"次回更新: {(int)remaining.TotalSeconds}秒後"
                    : "更新中...";

            foreach (var entry in RecordingEntries.ToList())
                entry.Tick();
        };
        timer.Start();
    }

    /// <summary>
    /// ビュー欄を作り直す。タグ設定を変えた後にも呼ぶ。
    /// 選択中のビューは、可能なら同じものを選び直す。
    /// </summary>
    public void RebuildViews()
    {
        var previous = SelectedView;

        Views.Clear();
        Views.Add(ChannelViewItem.ForKind(ChannelViewKind.All));
        Views.Add(ChannelViewItem.ForKind(ChannelViewKind.New));
        Views.Add(ChannelViewItem.ForKind(ChannelViewKind.Favorite));
        foreach (var tag in _settings.Current.Tags)
            Views.Add(ChannelViewItem.ForTag(tag));
        Views.Add(ChannelViewItem.ForKind(ChannelViewKind.Log));

        SelectedView =
            Views.FirstOrDefault(v => previous is not null
                && v.Kind == previous.Kind
                && v.Tag?.Id == previous.Tag?.Id)
            ?? Views[0];

        UpdateViewCounts();
    }

    private void UpdateViewCounts()
    {
        foreach (var view in Views)
        {
            // タグのビューは自分の隠しタグを常に見せるので、生の件数を出す
            view.Count = _filterService
                .Filter(_allChannels, view, "", ShowHidden || view.IncludesHidden)
                .Count;
        }
    }

    private void OnRecordingsChanged(object? sender, EventArgs e)
    {
        _dispatcher?.BeginInvoke(() =>
        {
            var activeIds = _recordService.ActiveRecordings
                .Select(r => r.ChannelId).ToHashSet();

            // 停止したエントリを非活性化（削除せずに残す）
            foreach (var entry in RecordingEntries.Where(r => r.IsActive && !activeIds.Contains(r.ChannelId)).ToList())
                entry.IsActive = false;

            // 新規エントリを追加（アクティブ中のものだけを重複チェック対象にする）
            var existingActiveIds = RecordingEntries
                .Where(r => r.IsActive).Select(r => r.ChannelId).ToHashSet();
            foreach (var entry in _recordService.ActiveRecordings.Where(r => !existingActiveIds.Contains(r.ChannelId)))
                RecordingEntries.Insert(0, entry);
        });
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

            bool isFirstFetch = _allChannels.Count == 0;

            _diffService.ApplyDiff(_allChannels, newList);
            ApplyTags(newList);

            var toNotify = _tagService.GetChannelsToNotify(newList);
            if (_settings.Current.Notifications.NotifyOnFavorite && toNotify.Count > 0)
                _notificationService.NotifyTaggedChannels(toNotify);

            if (!isFirstFetch)
            {
                var rules = Helpers.AutoDownloadSettingsMapper.ToRuleItems(_settings.Current.AutoDownloadRules);
                if (rules.Count > 0)
                {
                    foreach (var ch in _autoDownloadService.GetChannelsToAutoDownload(newList, rules))
                        _recordService.StartRecording(ch, _settings.Current.Downloader);
                }
            }

            _allChannels.Clear();
            _allChannels.AddRange(newList);
            _allChannels.AddRange(_diffService.GetAllLogChannels());

            ApplyFilter();
            UpdateTrayTooltip();

            IsRefreshing = false;
        });
    }

    private void ApplyTags(IEnumerable<ChannelItem> channels) =>
        _tagService.ApplyTags(channels, _settings.Current.Rules, _settings.Current.Tags);

    /// <summary>ルールやタグを編集した後に、次の更新を待たずに反映させる。</summary>
    public void ReapplyTags()
    {
        ApplyTags(_allChannels);
        RebuildViews();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var view = SelectedView ?? Views.FirstOrDefault();
        if (view is null) return;

        var filtered = _filterService.Filter(_allChannels, view, SearchText, ShowHidden);
        FilteredChannels.Clear();
        foreach (var ch in filtered)
            FilteredChannels.Add(ch);

        HiddenCount = _filterService.CountHidden(_allChannels, view, SearchText);
        UpdateViewCounts();
        OnPropertyChanged(nameof(ResultCountText));
    }

    /// <summary>
    /// トレイのツールチップだけを更新する。
    /// 件数はステータスバーには出さない（絞り込み欄の右に「N 件表示中 / M 件」があるため）。
    /// ステータスバー左は録音の操作結果を出すためだけに使う。
    /// </summary>
    private void UpdateTrayTooltip()
    {
        var total = LiveChannelCount;
        var listeners = _allChannels
            .Where(c => c.Diff != ChannelDiff.Log && c.Listeners > 0)
            .Sum(c => c.Listeners);
        _trayService.UpdateTooltip(total, listeners);
    }

    [RelayCommand]
    private async Task Refresh() => await _refreshService.RefreshNowAsync();

    [RelayCommand]
    private void ToggleShowHidden() => ShowHidden = !ShowHidden;

    [RelayCommand]
    private void OpenChannel(ChannelItem? channel)
    {
        if (channel == null) return;

        // タイプに合うプレイヤーが無ければ「その他」、それも無ければ OS の既定ハンドラ
        var player = PlayerSelection.For(_settings.Current.Players, channel.ChannelType);
        if (player == null)
        {
            _playerService.LaunchWithDefault(channel);
            return;
        }

        _playerService.Launch(channel, player);
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

    /// <summary>
    /// 星のトグル。チャンネル名の完全一致でお気に入りタグを付けるルールを自動生成／削除する。
    /// 手で書いたルールが同じチャンネルにお気に入りタグを付けている場合、星は消えない。
    /// </summary>
    [RelayCommand]
    private async Task ToggleStar(ChannelItem? channel)
    {
        if (channel == null) return;

        var rules = _settings.Current.Rules;
        var existing = rules.Where(r => r.IsStarRuleFor(channel.ChannelName)).ToList();

        if (existing.Count > 0)
        {
            foreach (var rule in existing) rules.Remove(rule);
        }
        else
        {
            var rule = Rule.CreateStarRule(channel.ChannelName);
            rule.Order = rules.Count == 0 ? 0 : rules.Max(r => r.Order) + 1;
            rules.Add(rule);
        }

        await _settings.SaveAsync();
        ReapplyTags();
    }

    [RelayCommand]
    private void ToggleRecord(ChannelItem? channel)
    {
        if (channel == null) return;
        if (_recordService.IsRecording(channel.Id))
        {
            _recordService.StopRecording(channel.Id);
            StatusText = $"録音停止: {channel.ChannelName}";
        }
        else
        {
            _recordService.StartRecording(channel, _settings.Current.Downloader);
            StatusText = $"録音開始: {channel.ChannelName}";
        }
    }

    [RelayCommand]
    private void StopRecord(string? channelId)
    {
        if (channelId == null) return;
        _recordService.StopRecording(channelId);
    }

    public bool IsChannelRecording(string channelId) => _recordService.IsRecording(channelId);

    /// <summary>右クリック「この条件でルールを作成」用。選択行の値を初期値に入れたルールを作る。</summary>
    public Rule CreateRuleFromChannel(ChannelItem channel)
    {
        var genre = string.Join(" ",
            new[] { channel.Genre, channel.Description }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new Rule
        {
            Name = string.IsNullOrWhiteSpace(genre) ? channel.ChannelName : genre,
            Order = _settings.Current.Rules.Count == 0 ? 0 : _settings.Current.Rules.Max(r => r.Order) + 1,
            Conditions =
            [
                new RuleCondition
                {
                    Field = ConditionField.Description,
                    MatchType = ConditionMatchType.Contains,
                    Pattern = genre,
                }
            ],
        };
    }

    /// <summary>ルール編集・タグ設定のライブ評価に使う、現在読み込んでいる一覧。</summary>
    public IReadOnlyList<ChannelItem> LiveChannels =>
        [.. _allChannels.Where(c => c.Diff != ChannelDiff.Log)];
}

public record OpenChannelWithArgs(ChannelItem? Channel, PlayerSettings? Player);
