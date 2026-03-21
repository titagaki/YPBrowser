// Standalone parser for unit tests (no WinUI dependency)
using System.Text.RegularExpressions;

namespace YPBrowser.Tests.Helpers;

public record ChannelRecord(
    string ChannelName, string Id, string Host, string ContactUrl,
    string Genre, string Description, int Listeners, int Relays,
    int BitrateKbps, string ChannelType,
    string TrackArtist, string TrackAlbum, string TrackTitle, string TrackGenre,
    string UrlParam, string BroadcastTimeStr, string KyasukoStatus,
    string Comment, bool IsDirect,
    string YpName, string YpUrl)
{
    public string StreamUrl => $"{YpUrl}pls/{Id}";
}

public static class YpParser
{
    public static List<ChannelRecord> ParseLines(string text, string ypName, string ypUrl)
    {
        var result = new List<ChannelRecord>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim('\r', '\n', ' ');
            if (string.IsNullOrEmpty(line)) continue;

            var f = line.Split(new[] { "<>" }, StringSplitOptions.None);
            if (f.Length < 19) continue;

            if (string.IsNullOrEmpty(f[1])) continue;

            result.Add(new ChannelRecord(
                HtmlDecode(Get(f, 0)), Get(f, 1), Get(f, 2),
                HtmlDecode(Get(f, 3)), HtmlDecode(Get(f, 4)), HtmlDecode(Get(f, 5)),
                ParseInt(Get(f, 6)), ParseInt(Get(f, 7)), ParseInt(Get(f, 8)),
                Get(f, 9),
                HtmlDecode(Get(f, 10)), HtmlDecode(Get(f, 11)), HtmlDecode(Get(f, 12)),
                HtmlDecode(Get(f, 13)), Get(f, 14), Get(f, 15), Get(f, 16),
                HtmlDecode(Get(f, 17)), Get(f, 18) == "1",
                ypName, ypUrl.TrimEnd('/') + "/"));
        }
        return result;
    }

    private static string Get(string[] f, int i) => i < f.Length ? f[i].Trim() : "";

    private static int ParseInt(string s) => int.TryParse(s, out var v) ? v : -1;

    private static string HtmlDecode(string s) => s
        .Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
        .Replace("&quot;", "\"").Replace("&apos;", "'").Replace("&nbsp;", " ");
}
