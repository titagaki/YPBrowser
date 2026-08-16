using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels.SettingsPages;

/// <summary>
/// YP 一覧の 1 行。編集対象の設定（複製）と、取得のたびに変わる実行時状態を束ねる。
///
/// 設定と状態を別々に持っているのは寿命が違うため。設定はキャンセルで捨てられるが、
/// 状態はアプリが持ち続ける。行の表示にはその両方が要るので、ここで組み合わせる。
/// </summary>
public partial class YpServerRow : ObservableObject
{
    private readonly IYpServerStateService _states;

    /// <summary>この行が編集している設定。OK を押したときに書き戻される実体。</summary>
    public YpServerSettings Settings { get; }

    public YpServerRow(YpServerSettings settings, IYpServerStateService states)
    {
        Settings = settings;
        _states = states;
        AttachState();
    }

    /// <summary>
    /// 対応する実行時状態。URL / ホストを変えると別の YP になるので、
    /// 編集のたびに引き直す（見つからなければ「未取得」扱い）。
    /// </summary>
    private YpServerItem? _state;

    public string Name => Settings.Name;

    public string Url => Settings.Url;

    public bool Enabled
    {
        get => Settings.Enabled;
        set
        {
            if (Settings.Enabled == value) return;
            Settings.Enabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    /// <summary>
    /// 取得の状況。無効な YP は取りに行かないので、状態ではなくその旨を出す
    /// （古い取得結果を出すと、いま動いているように見える）。
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            if (!Settings.Enabled) return "無効";
            return _state?.StatusDisplay ?? "未取得";
        }
    }

    public bool HasError => Settings.Enabled && _state?.HasError == true;

    /// <summary>編集ダイアログから戻ったあとに呼ぶ。接続先が変わっていれば状態も引き直す。</summary>
    public void Refresh()
    {
        AttachState();
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Url));
        OnPropertyChanged(nameof(Enabled));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(HasError));
    }

    /// <summary>
    /// 状態の変更（取得が終わった、失敗した）をそのまま行へ通す。
    /// 設定画面を開いたまま自動更新が走っても表示が古くならない。
    /// </summary>
    private void AttachState()
    {
        if (_state != null) _state.PropertyChanged -= OnStateChanged;

        _state = _states.Find(Settings);

        if (_state != null) _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(HasError));
    }
}
