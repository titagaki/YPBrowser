using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Abstractions;

public interface IRecordService
{
    void StartRecording(ChannelItem channel, DownloaderSettings settings);
    void StopRecording(string channelId);
    bool IsRecording(string channelId);
}
