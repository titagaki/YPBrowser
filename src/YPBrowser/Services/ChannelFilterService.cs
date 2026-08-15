using YPBrowser.Abstractions;
using YPBrowser.Models;

namespace YPBrowser.Services;

public class ChannelFilterService : IChannelFilterService
{
    public List<ChannelItem> Filter(
        IEnumerable<ChannelItem> channels,
        ChannelViewItem view,
        string searchText,
        bool showHidden)
    {
        var source = InView(channels, view);

        if (!showHidden && !view.IncludesHidden)
            source = source.Where(c => !c.IsHidden);

        return [.. ApplySearch(source, searchText)
            .OrderByDescending(c => c.IsHighlighted)
            .ThenByDescending(c => c.Listeners)];
    }

    public int CountHidden(IEnumerable<ChannelItem> channels, ChannelViewItem view, string searchText)
    {
        if (view.IncludesHidden) return 0;
        return ApplySearch(InView(channels, view), searchText).Count(c => c.IsHidden);
    }

    private static IEnumerable<ChannelItem> InView(IEnumerable<ChannelItem> channels, ChannelViewItem view) =>
        view.Kind switch
        {
            ChannelViewKind.New => channels.Where(c => c.IsNew),
            ChannelViewKind.Favorite => channels.Where(c => c.IsFavorite && c.Diff != ChannelDiff.Log),
            ChannelViewKind.Log => channels.Where(c => c.Diff == ChannelDiff.Log),
            ChannelViewKind.Tag => channels.Where(c =>
                c.Diff != ChannelDiff.Log && c.Tags.Any(t => t.Id == view.Tag!.Id)),
            _ => channels.Where(c => c.Diff != ChannelDiff.Log),
        };

    /// <summary>上部の絞り込み欄は単純な部分一致。正規表現は使わない。</summary>
    private static IEnumerable<ChannelItem> ApplySearch(IEnumerable<ChannelItem> source, string searchText)
    {
        var query = searchText.Trim();
        if (string.IsNullOrEmpty(query)) return source;

        return source.Where(c =>
            c.ChannelName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Genre.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Comment.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
