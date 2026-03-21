using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Abstractions;

public interface IRecordService
{
    void Record(ChannelItem channel, DownloaderSettings settings);
}
