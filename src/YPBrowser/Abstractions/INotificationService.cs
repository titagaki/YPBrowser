using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface INotificationService
{
    void Initialize();
    void SetWindowHandle(nint hwnd);
    void UpdateTrayTooltip(int channelCount, int totalListeners);
    void NotifyNewFavorites(IReadOnlyList<ChannelItem> newFavorites);
}
