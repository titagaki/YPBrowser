using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels;

/// <summary>
/// 設定ダイアログ全体の状態。
/// 編集するのは <see cref="SettingsDraft"/>（設定の複製）で、本体へ書き戻すのは「OK」のときだけ。
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    /// <summary>編集中の複製。ページはここから値を読み書きする。</summary>
    public SettingsDraft Draft { get; }

    public ObservableCollection<YpServerSettings> YpServers { get; } = [];
    public ObservableCollection<PlayerSettings> Players { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedYpServer))]
    private YpServerSettings? _selectedYpServer;
    public bool HasSelectedYpServer => SelectedYpServer != null;

    public AutoDownloadViewModel AutoDownload { get; }

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        Draft = SettingsDraft.From(settings.Current);
        AutoDownload = new AutoDownloadViewModel(Draft);

        foreach (var s in Draft.YpServers) YpServers.Add(s);
        foreach (var p in Draft.Players) Players.Add(p);
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

    /// <summary>タイプの並び順（「その他」は末尾）を保ったまま差し込む。</summary>
    public void AddPlayer(PlayerSettings player)
    {
        var key = PlayerContentTypes.SortKey(player.ContentType);

        var index = 0;
        while (index < Players.Count && PlayerContentTypes.SortKey(Players[index].ContentType) <= key)
            index++;

        Players.Insert(index, player);
    }

    public void RemovePlayer(PlayerSettings player) => Players.Remove(player);

    /// <summary>タイプを変えた後、並び順を直す。</summary>
    public void ReorderPlayer(PlayerSettings player)
    {
        if (Players.Remove(player)) AddPlayer(player);
    }

    /// <summary>
    /// すでに他のプレイヤーが担当しているタイプ。
    /// 1 タイプにつき 1 件だけにするため、編集ダイアログの選択肢から外す。
    /// </summary>
    public IReadOnlyList<string> UsedContentTypes(PlayerSettings? except) =>
        [.. Players.Where(p => !ReferenceEquals(p, except)).Select(p => p.ContentType)];

    /// <summary>「OK」で呼ぶ。複製を本体へ書き戻して保存する。</summary>
    public async Task ApplyAsync()
    {
        Draft.YpServers = [.. YpServers];
        Draft.Players = [.. Players];
        AutoDownload.Flush();

        Draft.ApplyTo(_settings.Current);
        await _settings.SaveAsync();
    }
}
