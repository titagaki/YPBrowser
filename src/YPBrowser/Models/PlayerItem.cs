using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

public partial class PlayerItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _executablePath = "";
    [ObservableProperty] private string _argumentTemplate = "\"{url}\"";
    [ObservableProperty] private bool _usePlaylistFile = false;
    [ObservableProperty] private bool _isDefault = false;
}
