using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Helpers;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

/// <summary>コンボボックスの選択肢 1 件。</summary>
/// <param name="Value">設定に書き込む値</param>
/// <param name="Label">画面に出す文字</param>
public record SettingOption<T>(T Value, string Label)
{
    /// <summary>
    /// レコードの既定の <c>ToString</c> は "SettingOption { Value = 0, Label = 更新しない }" を返す。
    /// 画面には `DisplayMemberPath` が効くので出ないが、支援技術はこちらを読み上げてしまう。
    /// </summary>
    public override string ToString() => Label;
}

/// <summary>
/// 「全般」ページ。旧「動作」と旧「プレイヤー」を吸収している。
/// 変更はすべて即時に <see cref="AppSettings"/> へ書き、保存を促す。
/// </summary>
public partial class GeneralPageViewModel : ObservableObject
{
    private readonly BehaviorSettings _behavior;

    public IReadOnlyList<SettingOption<StartupWindowState>> StartupStateOptions { get; } =
    [
        new(StartupWindowState.Normal, "通常表示"),
        new(StartupWindowState.Minimized, "最小化"),
        new(StartupWindowState.Tray, "トレイに格納"),
    ];

    public IReadOnlyList<SettingOption<MinimizeButtonAction>> MinimizeButtonActionOptions { get; } =
    [
        new(MinimizeButtonAction.KeepInTaskbar, "タスクバーに残す"),
        new(MinimizeButtonAction.MinimizeToTray, "トレイに格納"),
    ];

    /// <summary>
    /// 秒数を自由に入れられると、極端に短い値で YP に負荷をかける事故が起きる。
    /// 選べるのはここに並べた値だけ。取得側は間引かないので、ここが実アクセス間隔になる。
    /// </summary>
    public IReadOnlyList<SettingOption<int>> RefreshIntervalOptions { get; } =
    [
        .. SettingsMigration.RefreshIntervalPresets.Select(
            seconds => new SettingOption<int>(seconds, FormatInterval(seconds))),
    ];

    /// <summary>60 秒以上は「分」で見せる。"300 秒ごと" は間隔の見当が付きにくいため。</summary>
    private static string FormatInterval(int seconds) => seconds switch
    {
        <= 0 => "更新しない",
        < 60 => $"{seconds} 秒ごと",
        _ when seconds % 60 == 0 => $"{seconds / 60} 分ごと",
        _ => $"{seconds / 60} 分 {seconds % 60} 秒ごと",
    };

    /// <summary>外部プレイヤー。設定ダイアログ全体と同じ実体を見る。</summary>
    public ObservableCollection<PlayerSettings> Players { get; }

    [ObservableProperty] private StartupWindowState _startupState;
    [ObservableProperty] private MinimizeButtonAction _minimizeButtonAction;
    [ObservableProperty] private int _refreshIntervalSeconds;

    public GeneralPageViewModel(SettingsViewModel owner)
    {
        _behavior = owner.Draft.Behavior;
        Players = owner.Players;

        _startupState = _behavior.StartupState;
        _minimizeButtonAction = _behavior.MinimizeButtonAction;
        // 設定ファイルを手で書き換えられた場合に備えて、選択肢に無い値はここでも丸める
        _refreshIntervalSeconds = SettingsMigration.RoundRefreshInterval(_behavior.RefreshIntervalSeconds);
    }

    partial void OnStartupStateChanged(StartupWindowState value) => _behavior.StartupState = value;
    partial void OnMinimizeButtonActionChanged(MinimizeButtonAction value) => _behavior.MinimizeButtonAction = value;
    partial void OnRefreshIntervalSecondsChanged(int value) => _behavior.RefreshIntervalSeconds = value;
}
