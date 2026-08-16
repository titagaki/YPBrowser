using System.Text.Json;
using System.Text.Json.Serialization;

namespace YPBrowser.Settings;

/// <summary>
/// 設定ダイアログが編集する複製。
///
/// ダイアログを開いた時点で <see cref="AppSettings"/> を丸ごと写し取り、編集はこちらに対して行う。
/// 「OK」で初めて本体へ書き戻す。キャンセルは捨てるだけ。
/// 本体を直接書き換えていた頃は、一覧の増減しかキャンセルで戻らず、
/// 各項目の中身は戻らないという中途半端な状態だった。
///
/// タグとルールは別のダイアログが持ち主なので、ここでは触らない
/// （書き戻しの対象からも外してある）。
/// </summary>
public class SettingsDraft
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        // 複製にしか使わないので、読みやすさより往復して壊れないことを優先する
        Converters = { new JsonStringEnumConverter() },
    };

    public List<YpServerSettings> YpServers { get; set; } = [];
    public List<PlayerSettings> Players { get; set; } = [];
    public List<AutoDownloadRuleSettings> AutoDownloadRules { get; set; } = [];
    public DownloaderSettings Downloader { get; set; } = new();
    public NetworkSettings Network { get; set; } = new();
    public DisplaySettings Display { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();

    public static SettingsDraft From(AppSettings settings) => new()
    {
        YpServers = Clone(settings.YpServers),
        Players = Clone(settings.Players),
        AutoDownloadRules = Clone(settings.AutoDownloadRules),
        Downloader = Clone(settings.Downloader),
        Network = Clone(settings.Network),
        Display = Clone(settings.Display),
        Notifications = Clone(settings.Notifications),
        Behavior = Clone(settings.Behavior),
    };

    /// <summary>「OK」で呼ぶ。ここで初めて本体が変わる。</summary>
    public void ApplyTo(AppSettings settings)
    {
        settings.YpServers = YpServers;
        settings.Players = Players;
        settings.AutoDownloadRules = AutoDownloadRules;
        settings.Downloader = Downloader;
        settings.Network = Network;
        settings.Display = Display;
        settings.Notifications = Notifications;
        settings.Behavior = Behavior;
    }

    /// <summary>
    /// JSON で往復させて複製する。設定クラスはすべて JSON で保存できる形なので、
    /// 項目が増えてもここを直さなくて済む（手書きのコピーだと写し忘れる）。
    /// </summary>
    private static T Clone<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, CloneOptions), CloneOptions)!;
}
