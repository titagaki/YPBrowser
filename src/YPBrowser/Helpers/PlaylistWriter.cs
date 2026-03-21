using YPBrowser.Models;

namespace YPBrowser.Helpers;

public static class PlaylistWriter
{
    public static string BuildPls(ChannelItem channel)
    {
        var title = string.IsNullOrEmpty(channel.ChannelName) ? "Stream" : channel.ChannelName;
        return $"""
            [playlist]
            NumberOfEntries=1
            File1={channel.StreamUrl}
            Title1={title}
            Length1=-1
            Version=2
            """;
    }

    public static string BuildM3u(ChannelItem channel)
    {
        var title = string.IsNullOrEmpty(channel.ChannelName) ? "Stream" : channel.ChannelName;
        return $"#EXTM3U\n#EXTINF:-1,{title}\n{channel.StreamUrl}\n";
    }
}
