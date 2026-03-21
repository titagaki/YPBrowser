using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface IAutoDownloadMatchService
{
    List<ChannelItem> GetChannelsToAutoDownload(
        IEnumerable<ChannelItem> channels,
        IReadOnlyList<AutoDownloadRuleItem> rules);
}
