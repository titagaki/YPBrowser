using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using YPBrowser.Models;

namespace YPBrowser.Settings;

public class AppSettings
{
    public List<YpServerSettings> YpServers { get; set; } = [];

    /// <summary>タグ定義。並び順が行の色を決める優先順になる。</summary>
    public List<TagDefinition> Tags { get; set; } = [];

    /// <summary>判定ルール。上から順に評価される。</summary>
    public List<Rule> Rules { get; set; } = [];

    /// <summary>
    /// 旧「お気に入り」形式。読み込み時に <see cref="Tags"/> / <see cref="Rules"/> へ移行され、
    /// 移行後は空になる（保存時にファイルから消える）。
    /// </summary>
    public List<FavoriteSettings> Favorites { get; set; } = [];

    public List<PlayerSettings> Players { get; set; } = [];
    public List<AutoDownloadRuleSettings> AutoDownloadRules { get; set; } = [];
    public DownloaderSettings Downloader { get; set; } = new();
    public NetworkSettings Network { get; set; } = new();
    public DisplaySettings Display { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
}

public class YpServerSettings
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Host { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

/// <summary>旧形式のお気に入り1件。移行のためだけに残っている。</summary>
public class FavoriteSettings
{
    public string Title { get; set; } = "";
    public string Word { get; set; } = "";
    public List<string> TargetFields { get; set; } = ["ChannelName"];
    public bool IsRegex { get; set; } = false;
    public bool IsNG { get; set; } = false;
    public bool NotifyEnabled { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public string BackColor { get; set; } = "#FFFF99";
    public string TextColor { get; set; } = "#000000";
    public string SoundFile { get; set; } = "";
}

/// <summary>
/// 外部プレイヤー 1 件。コンテンツタイプ 1 つにつき 1 件を持つ。
/// 設定画面が一覧を出したまま編集モーダルで書き換えるので、変更を一覧へ返せるように
/// 通知を出す（他の設定クラスは通知を持たない素の入れ物のまま）。
/// </summary>
public partial class PlayerSettings : ObservableObject
{
    /// <summary>
    /// 担当するコンテンツタイプ（<c>FLV</c> など）。空文字は「その他」で、
    /// どのタイプにも当てはまらなかったチャンネルを引き受ける。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContentTypeLabel))]
    private string _contentType = PlayerContentTypes.Fallback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExecutableFileName))]
    private string _executablePath = "";

    /// <summary>引数の初期値。編集ダイアログは、これのままなら「まだ触っていない」と見なす。</summary>
    public const string DefaultArgumentTemplate = "\"{stream}\"";

    /// <summary>引数。使える置換子は <see cref="PlayerPlaceholders"/>。</summary>
    [ObservableProperty] private string _argumentTemplate = DefaultArgumentTemplate;

    /// <summary>一覧の左に出すタイプ表記。</summary>
    [JsonIgnore] public string ContentTypeLabel => PlayerContentTypes.Label(ContentType);

    /// <summary>一覧の見出し。プレイヤーに名前は付けさせず、実行ファイル名をそのまま出す。</summary>
    [JsonIgnore] public string ExecutableFileName => Path.GetFileName(ExecutablePath);

    /// <summary>
    /// 旧形式（プレイヤーごとに名前と「既定」フラグを持ち、タイプの区別が無かった頃）。
    /// 読み込み時に <see cref="ContentType"/> 方式へ移行され、以後は書き出されない。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>旧形式。移行の際、どのプレイヤーを「その他」に充てるかの判断にだけ使う。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsDefault { get; set; }
}

public class NetworkSettings
{
    /// <summary>
    /// YP 取得の制限時間。下限 5 秒。
    /// User-Agent はアプリが持つもので、ユーザーが指定する対象ではない（<c>App.xaml.cs</c>）。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}

public class DisplaySettings
{
    public string FontFamily { get; set; } = "Yu Gothic UI";
    public double FontSize { get; set; } = 13;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#1A1A1A";
    public string NewChannelColor { get; set; } = "#006600";
    public string SelectedColor { get; set; } = "#0078D4";
}

public class NotificationSettings
{
    public bool Enabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = false;
    public string SoundFile { get; set; } = "";
    public int BalloonTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// false で通知タグの新着通知を出さない（タグごとの <c>Notify</c> より優先）。
    /// 旧 <c>Behavior.NotifyOnFavorite</c>。
    /// </summary>
    public bool NotifyOnFavorite { get; set; } = true;
}

/// <summary>起動直後のウィンドウの状態。</summary>
public enum StartupWindowState
{
    Normal,
    Minimized,
    Tray,
}

/// <summary>最小化ボタンを押したときの行き先。閉じるボタンは常に終了で、設定は持たない。</summary>
public enum MinimizeButtonAction
{
    KeepInTaskbar,
    MinimizeToTray,
}

public class BehaviorSettings
{
    /// <summary>
    /// 自動更新間隔。<c>0</c> で自動更新しない。
    /// UI は <c>SettingsMigration.RefreshIntervalPresets</c> のプリセットしか選ばせない。
    /// 短すぎる値を手入力して YP に負荷をかける事故を防ぐため。
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 60;

    public StartupWindowState StartupState { get; set; } = StartupWindowState.Normal;

    public MinimizeButtonAction MinimizeButtonAction { get; set; } = MinimizeButtonAction.KeepInTaskbar;

    public int ActiveFilterIndex { get; set; } = 0;

    /// <summary>旧形式。読み込み時に <see cref="StartupState"/> へ移行され、以後は書き出されない。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StartMinimized { get; set; }

    /// <summary>旧形式。読み込み時に <see cref="MinimizeButtonAction"/> へ移行される。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? MinimizeToTray { get; set; }

    /// <summary>旧形式。読み込み時に <see cref="NotificationSettings.NotifyOnFavorite"/> へ移行される。</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NotifyOnFavorite { get; set; }
}

public class WindowSettings
{
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 600;
    public double X { get; set; } = -1;
    public double Y { get; set; } = -1;
    public double SplitterPosition { get; set; } = 150;
}

public class DownloaderSettings
{
    public string OutputDirectory { get; set; } = "";
    public string FileNameTemplate { get; set; } = "{channelName}_{timestamp}";
}

public class AutoDownloadRuleSettings
{
    public string Title { get; set; } = "";
    public string Word { get; set; } = "";
    public List<string> TargetFields { get; set; } = ["ChannelName"];
    public bool IsRegex { get; set; } = false;
    public bool Enabled { get; set; } = true;
}
