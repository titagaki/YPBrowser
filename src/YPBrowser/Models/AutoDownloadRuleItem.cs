using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

public partial class AutoDownloadRuleItem : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _word = "";
    [ObservableProperty] private MatchTargetFields _targetFields = MatchTargetFields.ChannelName;
    [ObservableProperty] private bool _isRegex = false;
    [ObservableProperty] private bool _enabled = true;
}
