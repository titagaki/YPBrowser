using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Media;

namespace YPBrowser.Models;

public partial class ChannelItem : ObservableObject
{
    // --- 19 fields from YP index.txt (separated by <>) ---
    [ObservableProperty] private string _channelName = "";
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _host = "";
    [ObservableProperty] private string _contactUrl = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GenreDescription))]
    private string _genre = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GenreDescription))]
    private string _description = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListenersRelaysDisplay))]
    private int _listeners = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListenersRelaysDisplay))]
    private int _relays = -1;
    [ObservableProperty] private int _bitrateKbps = 0;
    [ObservableProperty] private string _channelType = "";

    /// <summary>index.txt の 11 番目。UI では「Playing」として扱う。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GenreDescription))]
    [NotifyPropertyChangedFor(nameof(PlayingDisplay))]
    private string _trackArtist = "";

    [ObservableProperty] private string _trackAlbum = "";
    [ObservableProperty] private string _trackTitle = "";
    [ObservableProperty] private string _trackGenre = "";
    [ObservableProperty] private string _urlParam = "";
    [ObservableProperty] private string _broadcastTimeStr = "";
    [ObservableProperty] private string _kyasukoStatus = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GenreDescription))]
    private string _comment = "";
    [ObservableProperty] private bool _isDirect = false;

    // --- YP meta ---
    public string YpName { get; set; } = "";
    public string YpUrl { get; set; } = "";
    public string YpHost { get; set; } = "";  // ローカルPeerCastホスト (例: localhost:7144)
    public DateTime FetchedAt { get; set; }
    public int YpPriority { get; set; }

    // --- Set by diff engine ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    private ChannelDiff _diff = ChannelDiff.None;

    // --- Set by tag engine ---
    /// <summary>
    /// 判定結果。タグ設定での並び順にそろえてある（色を決めるとき「最初のもの」が使えるように）。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHidden))]
    [NotifyPropertyChangedFor(nameof(IsHighlighted))]
    [NotifyPropertyChangedFor(nameof(IsFavorite))]
    [NotifyPropertyChangedFor(nameof(TagBackColor))]
    [NotifyPropertyChangedFor(nameof(TagForeColor))]
    [NotifyPropertyChangedFor(nameof(TagNames))]
    private IReadOnlyList<TagDefinition> _tags = [];

    /// <summary>前回ポーリング時に存在しなかった。</summary>
    public bool IsNew => Diff == ChannelDiff.New;

    /// <summary>「一覧から隠す」タグを1つでも持つ。</summary>
    public bool IsHidden => Tags.Any(t => t.IsHidden);

    /// <summary>「強調して上へ」タグを1つでも持つ。</summary>
    public bool IsHighlighted => Tags.Any(t => t.IsHighlight);

    public bool IsFavorite => Tags.Any(t => t.Id == TagDefinition.FavoriteId);

    /// <summary>色が設定されている最初のタグの色を採用する（タグの並び順で決まる）。</summary>
    private TagDefinition? ColorTag => Tags.FirstOrDefault(t => t.HasColor);
    public Color? TagBackColor => ColorTag?.BackColorValue;
    public Color? TagForeColor => ColorTag?.ForeColorValue;

    public string TagNames => string.Join(" ", Tags.Select(t => t.Name));

    // --- Log state ---
    public DateTime LoggedAt { get; set; }

    // --- Computed ---
    public string StreamUrl
    {
        get
        {
            var localHost = string.IsNullOrEmpty(YpHost) ? "localhost:7144" : YpHost;
            var tip = string.IsNullOrEmpty(Host) ? "" : $"?tip={Host}";
            return $"http://{localHost}/pls/{Id}{tip}";
        }
    }
    public string DirectStreamUrl => string.IsNullOrEmpty(Host) ? "" : $"http://{Host}/pls/{Id}";
    public string StatsUrl => string.IsNullOrEmpty(YpUrl) ? ""
        : $"{YpUrl}getgmt.php?cn={Uri.EscapeDataString(ChannelName)}";
    public string ChatUrl => string.IsNullOrEmpty(YpUrl) ? ""
        : $"{YpUrl}chat.php?cn={Uri.EscapeDataString(ChannelName)}";

    /// <summary>
    /// 「Playing」欄の表示。空なら空文字（行に出さない）。
    /// アーティスト名とは限らず、配信中の曲名や配信経路が入ってくる。
    /// </summary>
    public string PlayingDisplay =>
        string.IsNullOrWhiteSpace(TrackArtist) ? "" : $"Playing: {TrackArtist}";

    /// <summary><c>[ジャンル - 説明] Playing: … 「コメント」</c>。空の要素はまるごと省く。</summary>
    public string GenreDescription
    {
        get
        {
            var base_ = string.Join(" - ", new[] { Genre, Description }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var bracketed = string.IsNullOrWhiteSpace(base_) ? "" : $"[{base_}]";
            var comment = string.IsNullOrWhiteSpace(Comment) ? "" : $"「{Comment}」";

            return string.Join(" ",
                new[] { bracketed, PlayingDisplay, comment }.Where(s => !string.IsNullOrEmpty(s)));
        }
    }

    public string ListenersDisplay => Listeners.ToString();
    public string RelaysDisplay => Relays.ToString();
    public string ListenersRelaysDisplay
        => $"{ListenersDisplay}/ {RelaysDisplay}";
    public string BitrateDisplay => BitrateKbps > 0 ? BitrateKbps.ToString() : "";

    public string TrackInfo
    {
        get
        {
            var parts = new[] { TrackArtist, TrackTitle, TrackAlbum }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(" / ", parts);
        }
    }
}
