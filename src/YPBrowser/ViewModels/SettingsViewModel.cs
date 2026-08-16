using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Settings;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.ViewModels;

/// <summary>
/// 設定ダイアログ全体の状態。
/// 編集するのは <see cref="SettingsDraft"/>（設定の複製）で、本体へ書き戻すのは「OK」のときだけ。
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IYpServerStateService _serverStates;

    /// <summary>編集中の複製。ページはここから値を読み書きする。</summary>
    public SettingsDraft Draft { get; }

    /// <summary>YP 一覧。設定だけでなく取得の状況も出すので、行 VM を挟む。</summary>
    public ObservableCollection<YpServerRow> YpServers { get; } = [];
    public ObservableCollection<PlayerSettings> Players { get; } = [];

    public AutoDownloadViewModel AutoDownload { get; }

    public SettingsViewModel(ISettingsService settings, IYpServerStateService serverStates)
    {
        _settings = settings;
        _serverStates = serverStates;
        Draft = SettingsDraft.From(settings.Current);
        AutoDownload = new AutoDownloadViewModel(Draft);

        foreach (var s in Draft.YpServers) YpServers.Add(new YpServerRow(s, serverStates));
        foreach (var p in Draft.Players) Players.Add(p);
    }

    public YpServerRow AddYpServer(YpServerSettings server)
    {
        var row = new YpServerRow(server, _serverStates);
        YpServers.Add(row);
        return row;
    }

    public void RemoveYpServer(YpServerRow row) => YpServers.Remove(row);

    /// <summary>取得は設定順なので、並べ替えは「どれを先に見に行くか」の指定になる。</summary>
    public void MoveYpServer(YpServerRow row, int offset)
    {
        var from = YpServers.IndexOf(row);
        if (from < 0) return;

        var to = from + offset;
        if (to < 0 || to >= YpServers.Count) return;

        YpServers.Move(from, to);
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
        Draft.YpServers = [.. YpServers.Select(row => row.Settings)];
        Draft.Players = [.. Players];
        AutoDownload.Flush();

        Draft.ApplyTo(_settings.Current);
        await _settings.SaveAsync();
    }
}
