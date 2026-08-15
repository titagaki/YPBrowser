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
    public int BitrateMin { get; set; } = 0;
    public int BitrateMax { get; set; } = -1;
    public string TypeFilter { get; set; } = ".*";
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

public class PlayerSettings
{
    public string Name { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string ArgumentTemplate { get; set; } = "\"{url}\"";
public bool IsDefault { get; set; } = false;
}

public class NetworkSettings
{
    public string ProxyUrl { get; set; } = "";
    public string UserAgent { get; set; } = "YPBrowser/1.0";
    public int TimeoutSeconds { get; set; } = 10;
}

public class DisplaySettings
{
    public string FontFamily { get; set; } = "Yu Gothic UI";
    public double FontSize { get; set; } = 13;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#1A1A1A";
    public string FavoriteNameColor { get; set; } = "#0000CC";
    public string NewChannelColor { get; set; } = "#006600";
    public string SelectedColor { get; set; } = "#0078D4";
}

public class NotificationSettings
{
    public bool Enabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = false;
    public string SoundFile { get; set; } = "";
    public int BalloonTimeoutSeconds { get; set; } = 5;
}

public class BehaviorSettings
{
    public int RefreshIntervalSeconds { get; set; } = 60;
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool OpenOnDoubleClick { get; set; } = true;
    public bool NotifyOnFavorite { get; set; } = true;
    public int ActiveFilterIndex { get; set; } = 0;
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
