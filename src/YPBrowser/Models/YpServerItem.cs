using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

/// <summary>
/// YP サーバー 1 件の実行時の姿。設定 (<c>YpServerSettings</c>) から写した値と、
/// 取得のたびに書き換わる状態を併せ持つ。
///
/// 実体は <c>IYpServerStateService</c> が保持する。取得のたびに作り直すと
/// 状態がどこにも残らないため、寿命はアプリと同じにしてある。
/// </summary>
public partial class YpServerItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _url = "";
    [ObservableProperty] private string _host = "";
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private int _bitrateMin = 0;
    [ObservableProperty] private int _bitrateMax = -1;
    [ObservableProperty] private string _typeFilter = ".*";

    // ここから下は実行時状態。設定ファイルには保存しない（再起動で消える）。

    /// <summary>最後に取得できた時刻。一度も成功していなければ <see cref="DateTime.MinValue"/>。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    [NotifyPropertyChangedFor(nameof(HasEverFetched))]
    private DateTime _lastUpdateTime = DateTime.MinValue;

    /// <summary>直近の取得が失敗したときの理由。成功したら <c>null</c> に戻す。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _lastError;

    /// <summary>最後に取得できたチャンネル数（サーバー単位のフィルタを通した後）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    private int _channelCount;

    public bool HasError => !string.IsNullOrEmpty(LastError);

    public bool HasEverFetched => LastUpdateTime != DateTime.MinValue;

    /// <summary>
    /// 設定画面の行に出す 1 行。取得できているか、できていないなら理由まで分かる形にする。
    /// 「静かにチャンネルが消えるだけ」を避けるのがこの表示の目的なので、失敗は必ず理由まで出す。
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            if (HasError)
                return HasEverFetched
                    ? $"取得できません: {LastError}（最終取得 {LastUpdateTime:HH:mm}）"
                    : $"取得できません: {LastError}";

            return HasEverFetched
                ? $"{LastUpdateTime:HH:mm:ss} 更新 ・ {ChannelCount:N0} 件"
                : "未取得";
        }
    }
}
