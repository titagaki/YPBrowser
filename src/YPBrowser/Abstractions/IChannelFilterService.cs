using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface IChannelFilterService
{
    IEnumerable<ChannelItem> Filter(IEnumerable<ChannelItem> channels, int filterIndex, string searchText);
}
