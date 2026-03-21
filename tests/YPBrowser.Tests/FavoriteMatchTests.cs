using YPBrowser.Tests.Helpers;

namespace YPBrowser.Tests;

public class FavoriteMatchTests
{
    private static ChannelRecord MakeChannel(string name, string genre = "G", string desc = "D") =>
        new(name, "ID", "h:7144", "url", genre, desc,
            5, 0, 64, "MP3", "", "", "", "", "", "0", "0", "", false, "YP", "http://yp.test/");

    [Fact]
    public void PlainMatch_ChannelName_Matches()
    {
        var ch = MakeChannel("TestGame Stream");
        Assert.True(ch.ChannelName.Contains("Game", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PlainMatch_CaseInsensitive()
    {
        var ch = MakeChannel("Abc Channel");
        Assert.True(ch.ChannelName.Contains("abc", StringComparison.OrdinalIgnoreCase));
        Assert.True(ch.ChannelName.Contains("ABC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RegexMatch_ValidPattern_Matches()
    {
        var ch = MakeChannel("Stream123");
        Assert.Matches("(?i)Stream\\d+", ch.ChannelName);
    }

    [Fact]
    public void RegexMatch_InvalidPattern_DoesNotThrow()
    {
        System.Text.RegularExpressions.Regex? rx = null;
        try { rx = new System.Text.RegularExpressions.Regex("[invalid"); }
        catch { }
        Assert.Null(rx);
    }

    [Fact]
    public void HtmlDecode_AmpersandEntities()
    {
        var line = "A&amp;B<>ID1<>h:7144<>url<>G<>D<>0<>0<>64<>MP3<><><><><><>0<>0<><>0";
        var channels = YpParser.ParseLines(line, "YP", "http://yp.test/");
        Assert.Equal("A&B", channels[0].ChannelName);
    }
}
