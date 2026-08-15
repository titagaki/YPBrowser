using YPBrowser.Models;

namespace YPBrowser.Tests;

/// <summary>一覧の 2 段目に出る要約行の書式。</summary>
public class ChannelItemDisplayTests
{
    private static ChannelItem Channel(
        string genre = "", string description = "", string comment = "", string trackArtist = "") =>
        new()
        {
            ChannelName = "ハギch",
            Genre = genre,
            Description = description,
            Comment = comment,
            TrackArtist = trackArtist,
        };

    [Fact]
    public void GenreDescription_PutsPlayingBetweenTheBracketAndTheComment()
    {
        var ch = Channel(
            genre: "PS5",
            description: "ファイナルファンタジー14 - <Free>",
            comment: "紅蓮のリベレーター4.1から",
            trackArtist: "110.233.212.225 via Peercast Gateway");

        Assert.Equal(
            "[PS5 - ファイナルファンタジー14 - <Free>] Playing: 110.233.212.225 via Peercast Gateway 「紅蓮のリベレーター4.1から」",
            ch.GenreDescription);
    }

    [Fact]
    public void PlayingDisplay_IsEmptyWhenTheSlotIsEmpty()
    {
        Assert.Equal("", Channel().PlayingDisplay);
        Assert.Equal("", Channel(trackArtist: "   ").PlayingDisplay);
    }

    [Fact]
    public void PlayingDisplay_PrefixesTheValue()
    {
        Assert.Equal(
            "Playing: 110.233.212.225 via Peercast Gateway",
            Channel(trackArtist: "110.233.212.225 via Peercast Gateway").PlayingDisplay);
    }

    [Fact]
    public void GenreDescription_OmitsPlayingWhenEmpty()
    {
        var ch = Channel(genre: "ゲーム", description: "配信中", comment: "よろしく");
        Assert.Equal("[ゲーム - 配信中] 「よろしく」", ch.GenreDescription);
    }

    [Fact]
    public void GenreDescription_ShowsPlayingAloneWhenNothingElseIsSet()
    {
        Assert.Equal("Playing: 曲名", Channel(trackArtist: "曲名").GenreDescription);
    }

    [Fact]
    public void GenreDescription_OmitsTheBracketWhenGenreAndDescriptionAreEmpty()
    {
        var ch = Channel(comment: "コメント", trackArtist: "曲名");
        Assert.Equal("Playing: 曲名 「コメント」", ch.GenreDescription);
    }

    [Fact]
    public void GenreDescription_IsEmptyWhenEverythingIsEmpty()
    {
        // 空のときは行から消える（XAML 側の DataTrigger が Value="" を見ている）
        Assert.Equal("", Channel().GenreDescription);
    }

    [Fact]
    public void GenreDescription_JoinsGenreAndDescriptionWithADash()
    {
        Assert.Equal("[ゲーム - 配信中]", Channel(genre: "ゲーム", description: "配信中").GenreDescription);
        Assert.Equal("[ゲーム]", Channel(genre: "ゲーム").GenreDescription);
        Assert.Equal("[配信中]", Channel(description: "配信中").GenreDescription);
    }
}
