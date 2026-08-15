using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Media;

namespace YPBrowser.Helpers;

/// <summary>
/// `#RRGGBB` 形式のテキストと <see cref="Color"/> の相互変換。
/// タグの色は毎ポーリングで全チャンネル分参照されるため、パース結果をキャッシュする。
/// </summary>
public static class ColorHelper
{
    private static readonly ConcurrentDictionary<string, Color?> Cache = new();

    /// <summary>パースできない場合は null（色指定なし）を返す。</summary>
    public static Color? Parse(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        return Cache.GetOrAdd(hex, static h =>
        {
            var s = h.Trim().TrimStart('#');
            if (s.Length != 6) return null;
            if (!byte.TryParse(s[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
                !byte.TryParse(s[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
                !byte.TryParse(s[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                return null;
            return Color.FromRgb(r, g, b);
        });
    }

    public static string ToHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool IsValid(string? hex) => Parse(hex) is not null;
}
