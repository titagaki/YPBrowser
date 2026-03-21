using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace YPBrowser.Models;

public partial class ChannelItem : ObservableObject
{
    // --- 19 fields from YP index.txt (separated by <>) ---
    [ObservableProperty] private string _channelName = "";
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _host = "";
    [ObservableProperty] private string _contactUrl = "";
    [ObservableProperty] private string _genre = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private int _listeners = -1;
    [ObservableProperty] private int _relays = -1;
    [ObservableProperty] private int _bitrateKbps = 0;
    [ObservableProperty] private string _channelType = "";
    [ObservableProperty] private string _trackArtist = "";
    [ObservableProperty] private string _trackAlbum = "";
    [ObservableProperty] private string _trackTitle = "";
    [ObservableProperty] private string _trackGenre = "";
    [ObservableProperty] private string _urlParam = "";
    [ObservableProperty] private string _broadcastTimeStr = "";
    [ObservableProperty] private string _kyasukoStatus = "";
    [ObservableProperty] private string _comment = "";
    [ObservableProperty] private bool _isDirect = false;

    // --- YP meta ---
    public string YpName { get; set; } = "";
    public string YpUrl { get; set; } = "";
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
    public string StreamUrl => string.IsNullOrEmpty(YpUrl) ? "" : $"{YpUrl}pls/{Id}";
    public string DirectStreamUrl => string.IsNullOrEmpty(Host) ? "" : $"http://{Host}/pls/{Id}";

    public string ListenersDisplay => Listeners >= 0 ? Listeners.ToString() : "?";
    public string RelaysDisplay => Relays >= 0 ? Relays.ToString() : "?";
    public string BitrateDisplay => BitrateKbps > 0 ? $"{BitrateKbps}k" : "";

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
