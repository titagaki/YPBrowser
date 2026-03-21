using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface IFavoriteMatchService
{
    void MatchAll(IEnumerable<ChannelItem> channels, IReadOnlyList<FavoriteItem> favorites);
    bool Match(ChannelItem ch, FavoriteItem fav);
    List<ChannelItem> GetNewFavoriteChannels(IEnumerable<ChannelItem> channels);
}
