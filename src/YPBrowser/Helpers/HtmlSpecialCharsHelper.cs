using System.Text.RegularExpressions;

namespace YPBrowser.Helpers;

public static partial class HtmlSpecialCharsHelper
{
    [GeneratedRegex(@"&#(\d+);")]
    private static partial Regex DecimalEntityRegex();

    [GeneratedRegex(@"&#x([0-9a-fA-F]+);")]
    private static partial Regex HexEntityRegex();

    public static string Decode(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        input = input
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'")
            .Replace("&nbsp;", " ");

        input = DecimalEntityRegex().Replace(input, m =>
            ((char)int.Parse(m.Groups[1].Value)).ToString());

        input = HexEntityRegex().Replace(input, m =>
            ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());

        return input;
    }
}
