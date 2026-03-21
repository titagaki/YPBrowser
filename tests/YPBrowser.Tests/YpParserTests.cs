using System.Text;
using YPBrowser.Tests.Helpers;

namespace YPBrowser.Tests;

public class YpParserTests
{
    [Fact]
    public void ParseLines_ValidLine_ReturnsChannel()
    {
        var line = "TestChannel<>ABCD1234ABCD1234ABCD1234ABCD1234<>192.168.1.1:7144<>http://example.com<>ゲーム<>テスト配信<>5<>2<>128<>MP3<>Artist<>Album<>Title<>Genre<><>120:00<>0<>コメント<>0";
        var channels = YpParser.ParseLines(line + "\n", "TestYP", "http://yp.test/");

        Assert.Single(channels);
        var ch = channels[0];
        Assert.Equal("TestChannel", ch.ChannelName);
        Assert.Equal("ABCD1234ABCD1234ABCD1234ABCD1234", ch.Id);
        Assert.Equal("ゲーム", ch.Genre);
        Assert.Equal(5, ch.Listeners);
        Assert.Equal(128, ch.BitrateKbps);
        Assert.Equal("MP3", ch.ChannelType);
        Assert.False(ch.IsDirect);
    }

    [Fact]
    public void ParseLines_EmptyLine_SkipsLine()
    {
        var channels = YpParser.ParseLines("\n\n\n", "YP", "http://yp.test/");
        Assert.Empty(channels);
    }

    [Fact]
    public void ParseLines_HtmlEntities_Decoded()
    {
        var line = "Test&amp;Channel<>ID123<>host:7144<>http://x.com<>Genre&lt;1&gt;<>Desc<>0<>0<>64<>OGG<><><><><><>0<>0<><>0";
        var channels = YpParser.ParseLines(line, "YP", "http://yp.test/");
        Assert.Single(channels);
        Assert.Equal("Test&Channel", channels[0].ChannelName);
        Assert.Equal("Genre<1>", channels[0].Genre);
    }

    [Fact]
    public void ParseLines_InsufficientFields_SkipsLine()
    {
        var line = "OnlyTwoFields<>ID";
        var channels = YpParser.ParseLines(line, "YP", "http://yp.test/");
        Assert.Empty(channels);
    }

    [Fact]
    public void ParseLines_MultipleChannels_ReturnsAll()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 5; i++)
            sb.AppendLine($"Ch{i}<>ID{i:D8}XXXXXXXXXXXXXXXXXXXXXXXX<>host:7144<>http://x.com<>G<>D<>1<>0<>64<>MP3<><><><><><>0<>0<><>0");
        var channels = YpParser.ParseLines(sb.ToString(), "YP", "http://yp.test/");
        Assert.Equal(5, channels.Count);
    }

    [Fact]
    public void StreamUrl_FormatIsCorrect()
    {
        var line = "Ch<>AABBCCDD11223344AABBCCDD11223344<>h:7144<>url<>G<>D<>0<>0<>64<>MP3<><><><><><>0<>0<><>0";
        var channels = YpParser.ParseLines(line, "YP", "http://yp.test/");
        Assert.Single(channels);
        Assert.Equal("http://yp.test/pls/AABBCCDD11223344AABBCCDD11223344", channels[0].StreamUrl);
    }
}
