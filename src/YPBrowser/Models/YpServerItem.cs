using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

public partial class YpServerItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _url = "";
    [ObservableProperty] private string _host = "";
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private int _bitrateMin = 0;
    [ObservableProperty] private int _bitrateMax = -1;
    [ObservableProperty] private string _typeFilter = ".*";

    // Transient state (not persisted)
    public DateTime LastUpdateTime { get; set; } = DateTime.MinValue;
    public string? LastError { get; set; }
    public int ChannelCount { get; set; }
}
