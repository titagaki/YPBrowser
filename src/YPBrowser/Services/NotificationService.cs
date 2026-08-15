using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;
using YPBrowser.Models;

namespace YPBrowser.Services;

public class NotificationService : INotificationService, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly ILogger<NotificationService> _logger;
    private bool _disposed;
    private nint _hwnd;

    public event EventHandler? ShowWindowRequested;
    public event EventHandler? ExitRequested;

    public NotificationService(ISettingsService settings, ILogger<NotificationService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Notification service initialized");
    }

    public void SetWindowHandle(nint hwnd)
    {
        _hwnd = hwnd;
    }

    public void UpdateTrayTooltip(int channelCount, int totalListeners)
    {
        // Future: update tray icon tooltip via Shell_NotifyIcon
    }

    public void NotifyTaggedChannels(IReadOnlyList<ChannelItem> channels)
    {
        if (!_settings.Current.Notifications.Enabled) return;
        if (channels.Count == 0) return;

        foreach (var ch in channels.Take(5))
        {
            // 通知を出したタグのうち先頭のものを見出しに使う（音の指定も同じタグから取る）
            var tag = ch.Tags.FirstOrDefault(t => t.Notify);
            if (tag is null) continue;

            _logger.LogInformation("Tagged channel appeared: {Tag} / {Name}", tag.Name, ch.ChannelName);
            TryShowToast(ch, tag);
        }
    }

    private void TryShowToast(ChannelItem ch, TagDefinition tag)
    {
        try
        {
            var sound = ResolveSound(tag);
            var xml = $@"<toast>
  <visual>
    <binding template=""ToastGeneric"">
      <text>YPBrowser - {EscapeXml(tag.Name)}</text>
      <text>{EscapeXml(ch.ChannelName)} ({ch.BitrateDisplay} {ch.ChannelType})</text>
      <text>{EscapeXml(ch.Description)}</text>
    </binding>
  </visual>
  {sound}
</toast>";
            var doc = new Windows.Data.Xml.Dom.XmlDocument();
            doc.LoadXml(xml);
            var toast = new Windows.UI.Notifications.ToastNotification(doc);
            var notifier = Windows.UI.Notifications.ToastNotificationManager
                .CreateToastNotifier("YPBrowser");
            notifier.Show(toast);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Toast notification failed");
        }
    }

    /// <summary>
    /// タグに通知音が指定されていればトーストの音を止め、その wav を自前で鳴らす。
    /// パッケージ化されていない Win32 アプリでは、トーストの audio src に任意のパスを渡せないため。
    /// </summary>
    private string ResolveSound(TagDefinition tag)
    {
        var path = tag.SoundPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return "";  // 既定音

        try
        {
            using var player = new System.Media.SoundPlayer(path);
            player.Play();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to play notification sound {Path}", path);
            return "";
        }
        return @"<audio silent=""true"" />";
    }

    private static string EscapeXml(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
