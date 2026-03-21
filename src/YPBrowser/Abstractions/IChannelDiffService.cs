using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface IChannelDiffService
{
    void ApplyDiff(IList<ChannelItem> oldList, IList<ChannelItem> newList);
    IReadOnlyList<ChannelItem> GetLogChannels(string ypName);
    IReadOnlyList<ChannelItem> GetAllLogChannels();
}
