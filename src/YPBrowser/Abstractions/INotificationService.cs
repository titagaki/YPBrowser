using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface INotificationService
{
    void Initialize();
    void SetWindowHandle(nint hwnd);
    void UpdateTrayTooltip(int channelCount, int totalListeners);
    /// <summary>通知が有効なタグが付いた新着チャンネルを知らせる。</summary>
    void NotifyTaggedChannels(IReadOnlyList<ChannelItem> channels);
}
