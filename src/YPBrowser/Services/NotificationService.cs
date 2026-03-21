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

    public void NotifyNewFavorites(IReadOnlyList<ChannelItem> newFavorites)
    {
        if (!_settings.Current.Notifications.Enabled) return;
        if (newFavorites.Count == 0) return;

        foreach (var ch in newFavorites.Take(5))
        {
            _logger.LogInformation("Favorite channel appeared: {Name}", ch.ChannelName);
            TryShowToast(ch);
        }
    }

    private void TryShowToast(ChannelItem ch)
    {
        try
        {
            var xml = $@"<toast>
  <visual>
    <binding template=""ToastGeneric"">
      <text>YPBrowser - お気に入り新着</text>
      <text>{EscapeXml(ch.ChannelName)} ({ch.BitrateDisplay} {ch.ChannelType})</text>
      <text>{EscapeXml(ch.Description)}</text>
    </binding>
  </visual>
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
