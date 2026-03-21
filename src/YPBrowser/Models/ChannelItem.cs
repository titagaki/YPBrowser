using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;
using System;

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
    [ObservableProperty] private string _trackArtist = "";
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

    // --- Set by diff/favorite engine ---
    [ObservableProperty] private ChannelDiff _diff = ChannelDiff.None;
    [ObservableProperty] private bool _isFavorite = false;
    [ObservableProperty] private bool _isNG = false;
    [ObservableProperty] private Color? _favBackColor;
    [ObservableProperty] private Color? _favTextColor;
    [ObservableProperty] private int _favPriority = -1;

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

    public string GenreDescription
    {
        get
        {
            var base_ = string.Join(" - ", new[] { Genre, Description }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var bracketed = string.IsNullOrWhiteSpace(base_) ? "" : $"[{base_}]";
            return string.IsNullOrWhiteSpace(Comment)
                ? bracketed
                : $"{bracketed} 「{Comment}」";
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
