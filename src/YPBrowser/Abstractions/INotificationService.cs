using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface INotificationService
{
    void Initialize();

    /// <summary>通知が有効なタグが付いた新着チャンネルを知らせる。</summary>
    void NotifyTaggedChannels(IReadOnlyList<ChannelItem> channels);
}
