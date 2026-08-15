using YPBrowser.Models;
using YPBrowser.Services;

namespace YPBrowser.Tests;

public class ChannelFilterServiceTests
{
    private static readonly TagDefinition Favorite = TagDefinition.CreateFavorite();
    private static readonly TagDefinition Ng = TagDefinition.CreateNg();
    private static readonly TagDefinition Music = new() { Name = "音楽" };

    private static ChannelItem Channel(
        string name,
        int listeners = 0,
        ChannelDiff diff = ChannelDiff.None,
        params TagDefinition[] tags) =>
        new()
        {
            ChannelName = name,
            Listeners = listeners,
            Diff = diff,
            Tags = tags,
        };

    private static readonly ChannelItem Plain = Channel("普通", 10);
    private static readonly ChannelItem Fresh = Channel("新着", 5, ChannelDiff.New);
    private static readonly ChannelItem Starred = Channel("お気に入り", 1, ChannelDiff.None, Favorite);
    private static readonly ChannelItem Blocked = Channel("NGなやつ", 99, ChannelDiff.None, Ng);
    private static readonly ChannelItem Tagged = Channel("音楽ch", 20, ChannelDiff.None, Music);
    private static readonly ChannelItem Gone = Channel("消えた", 3, ChannelDiff.Log);

    private static List<ChannelItem> All => [Plain, Fresh, Starred, Blocked, Tagged, Gone];

    private static ChannelViewItem View(ChannelViewKind kind) => ChannelViewItem.ForKind(kind);

    [Fact]
    public void AllView_ExcludesLogAndHidden()
    {
        var result = new ChannelFilterService().Filter(All, View(ChannelViewKind.All), "", showHidden: false);

        Assert.DoesNotContain(result, c => c.ChannelName == "消えた");
        Assert.DoesNotContain(result, c => c.ChannelName == "NGなやつ");
        Assert.Contains(result, c => c.ChannelName == "普通");
    }

    [Fact]
    public void ShowHidden_BringsHiddenChannelsBack()
    {
        var result = new ChannelFilterService().Filter(All, View(ChannelViewKind.All), "", showHidden: true);
        Assert.Contains(result, c => c.ChannelName == "NGなやつ");
    }

    [Fact]
    public void NewView_OnlyNewChannels()
    {
        var result = new ChannelFilterService().Filter(All, View(ChannelViewKind.New), "", showHidden: false);
        var only = Assert.Single(result);
        Assert.Equal("新着", only.ChannelName);
    }

    [Fact]
    public void FavoriteView_OnlyChannelsWithTheBuiltInFavoriteTag()
    {
        var result = new ChannelFilterService().Filter(All, View(ChannelViewKind.Favorite), "", showHidden: false);
        var only = Assert.Single(result);
        Assert.Equal("お気に入り", only.ChannelName);
    }

    [Fact]
    public void LogView_OnlyLogChannels()
    {
        var result = new ChannelFilterService().Filter(All, View(ChannelViewKind.Log), "", showHidden: false);
        var only = Assert.Single(result);
        Assert.Equal("消えた", only.ChannelName);
    }

    [Fact]
    public void TagView_ShowsChannelsWithThatTag()
    {
        var result = new ChannelFilterService()
            .Filter(All, ChannelViewItem.ForTag(Music), "", showHidden: false);

        var only = Assert.Single(result);
        Assert.Equal("音楽ch", only.ChannelName);
    }

    /// <summary>NG の中身は NG のビューから確認できる。</summary>
    [Fact]
    public void HiddenTagView_ShowsItsOwnChannelsEvenWhenNotShowingHidden()
    {
        var result = new ChannelFilterService()
            .Filter(All, ChannelViewItem.ForTag(Ng), "", showHidden: false);

        var only = Assert.Single(result);
        Assert.Equal("NGなやつ", only.ChannelName);
    }

    [Fact]
    public void Search_IsPlainSubstringNotRegex()
    {
        var svc = new ChannelFilterService();
        var view = View(ChannelViewKind.All);

        Assert.Contains(svc.Filter(All, view, "音楽", false), c => c.ChannelName == "音楽ch");
        // 正規表現として解釈されない
        Assert.Empty(svc.Filter(All, view, "音.ch", false));
    }

    [Fact]
    public void HighlightedChannelsSortFirstThenByListeners()
    {
        var svc = new ChannelFilterService();
        var result = svc.Filter(All, View(ChannelViewKind.All), "", showHidden: false);

        // お気に入りは視聴者 1 人でも先頭
        Assert.Equal("お気に入り", result[0].ChannelName);
        Assert.Equal(["音楽ch", "普通", "新着"], result.Skip(1).Select(c => c.ChannelName));
    }

    [Fact]
    public void CountHidden_ReportsWhatTheViewIsSuppressing()
    {
        var svc = new ChannelFilterService();
        Assert.Equal(1, svc.CountHidden(All, View(ChannelViewKind.All), ""));
    }

    /// <summary>絞り込みで消えた分は「非表示中」に数えない（別の理由で消えているため）。</summary>
    [Fact]
    public void CountHidden_RespectsTheSearchText()
    {
        var svc = new ChannelFilterService();
        Assert.Equal(0, svc.CountHidden(All, View(ChannelViewKind.All), "音楽"));
    }

    [Fact]
    public void CountHidden_IsZeroInAHiddenTagsOwnView()
    {
        var svc = new ChannelFilterService();
        Assert.Equal(0, svc.CountHidden(All, ChannelViewItem.ForTag(Ng), ""));
    }
}
