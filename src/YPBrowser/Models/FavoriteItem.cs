using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

[Flags]
public enum FavoriteTargetFields
{
    None = 0,
    ChannelName = 1 << 0,
    Genre = 1 << 1,
    Description = 1 << 2,
    Comment = 1 << 3,
    ContactUrl = 1 << 4,
    YpName = 1 << 5,
    ChannelType = 1 << 6,
    TrackTitle = 1 << 7,
    TrackArtist = 1 << 8,
    All = ChannelName | Genre | Description | Comment | ContactUrl | YpName | ChannelType | TrackTitle | TrackArtist,
}

public partial class FavoriteItem : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _word = "";
    [ObservableProperty] private FavoriteTargetFields _targetFields = FavoriteTargetFields.ChannelName;
    [ObservableProperty] private bool _isRegex = false;
    [ObservableProperty] private bool _isNG = false;
    [ObservableProperty] private bool _notifyEnabled = true;
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private string _backColor = "#FFFF99";
    [ObservableProperty] private string _textColor = "#000000";
    [ObservableProperty] private string _soundFile = "";
}
