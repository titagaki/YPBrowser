using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels;

public partial class AutoDownloadViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    public DownloaderSettings Downloader => _settings.Current.Downloader;

    public ObservableCollection<AutoDownloadRuleSettings> Rules { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRule))]
    private AutoDownloadRuleSettings? _selectedRule;
    public bool HasSelectedRule => SelectedRule != null;

    public AutoDownloadViewModel(ISettingsService settings)
    {
        _settings = settings;
        Load();
    }

    private void Load()
    {
        Rules.Clear();
        foreach (var r in _settings.Current.AutoDownloadRules) Rules.Add(r);
    }

    [RelayCommand]
    private void AddRule()
    {
        var r = new AutoDownloadRuleSettings { Title = "新しいルール", Word = "", Enabled = true };
        Rules.Add(r);
        SelectedRule = r;
    }

    [RelayCommand]
    private void RemoveRule()
    {
        if (SelectedRule != null) Rules.Remove(SelectedRule);
    }

    [RelayCommand]
    private void MoveRuleUp()
    {
        if (SelectedRule == null) return;
        var idx = Rules.IndexOf(SelectedRule);
        if (idx > 0) Rules.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveRuleDown()
    {
        if (SelectedRule == null) return;
        var idx = Rules.IndexOf(SelectedRule);
        if (idx < Rules.Count - 1) Rules.Move(idx, idx + 1);
    }

    public void Flush()
    {
        _settings.Current.AutoDownloadRules = [.. Rules];
    }
}
