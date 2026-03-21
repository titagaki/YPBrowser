using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    public AppSettings Current => _settings.Current;

    public ObservableCollection<YpServerSettings> YpServers { get; } = [];
    public ObservableCollection<PlayerSettings> Players { get; } = [];

    [ObservableProperty] private YpServerSettings? _selectedYpServer;
    [ObservableProperty] private PlayerSettings? _selectedPlayer;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        Load();
    }

    private void Load()
    {
        YpServers.Clear();
        foreach (var s in _settings.Current.YpServers) YpServers.Add(s);
        Players.Clear();
        foreach (var p in _settings.Current.Players) Players.Add(p);
    }

    [RelayCommand]
    private void AddYpServer()
    {
        var s = new YpServerSettings { Name = "新しいYP", Url = "http://", Enabled = true };
        YpServers.Add(s);
        SelectedYpServer = s;
    }

    [RelayCommand]
    private void RemoveYpServer()
    {
        if (SelectedYpServer != null)
            YpServers.Remove(SelectedYpServer);
    }

    [RelayCommand]
    private void MoveYpServerUp()
    {
        if (SelectedYpServer == null) return;
        var idx = YpServers.IndexOf(SelectedYpServer);
        if (idx > 0) YpServers.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveYpServerDown()
    {
        if (SelectedYpServer == null) return;
        var idx = YpServers.IndexOf(SelectedYpServer);
        if (idx < YpServers.Count - 1) YpServers.Move(idx, idx + 1);
    }

    [RelayCommand]
    private void AddPlayer()
    {
        var p = new PlayerSettings { Name = "新しいプレイヤー", ArgumentTemplate = "\"{url}\"" };
        Players.Add(p);
        SelectedPlayer = p;
    }

    [RelayCommand]
    private void RemovePlayer()
    {
        if (SelectedPlayer != null)
            Players.Remove(SelectedPlayer);
    }

    public async Task SaveAsync()
    {
        _settings.Current.YpServers = [.. YpServers];
        _settings.Current.Players = [.. Players];
        await _settings.SaveAsync();
    }
}
