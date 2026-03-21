using YPBrowser.Abstractions;
using YPBrowser.Models;

namespace YPBrowser.Services;

public class ChannelFilterService : IChannelFilterService
{
    public IEnumerable<ChannelItem> Filter(IEnumerable<ChannelItem> channels, int filterIndex, string searchText)
    {
        IEnumerable<ChannelItem> source = filterIndex switch
        {
            1 => channels.Where(c => c.Diff == ChannelDiff.New),
            2 => channels.Where(c => c.IsFavorite && !c.IsNG),
            3 => channels.Where(c => c.IsNG),
            4 => channels.Where(c => c.Diff == ChannelDiff.Log),
            _ => channels.Where(c => c.Diff != ChannelDiff.Log && !c.IsNG),
        };

        var query = searchText.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            source = source.Where(c =>
                c.ChannelName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Genre.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Comment.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return source
            .OrderByDescending(c => c.IsFavorite ? 1 : 0)
            .ThenByDescending(c => c.Listeners);
    }
}
