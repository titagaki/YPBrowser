using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface IChannelFilterService
{
    /// <summary>ビューと絞り込み文字列を適用し、表示順に並べて返す。</summary>
    List<ChannelItem> Filter(
        IEnumerable<ChannelItem> channels,
        ChannelViewItem view,
        string searchText,
        bool showHidden);

    /// <summary>
    /// そのビューで「一覧から隠す」タグによって伏せられている件数。
    /// 絞り込み文字列も考慮する（フィルタで消えた配信と混同させないため）。
    /// </summary>
    int CountHidden(IEnumerable<ChannelItem> channels, ChannelViewItem view, string searchText);
}
