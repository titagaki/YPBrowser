using System.Text.RegularExpressions;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Services;

public class FavoriteMatchService : IFavoriteMatchService
{
    private readonly Dictionary<string, Regex?> _regexCache = [];

    public void MatchAll(IEnumerable<ChannelItem> channels, IReadOnlyList<FavoriteItem> favorites)
    {
        foreach (var ch in channels)
        {
            ch.IsFavorite = false;
            ch.IsNG = false;
            ch.FavBackColor = null;
            ch.FavTextColor = null;
            ch.FavPriority = -1;

            for (int i = 0; i < favorites.Count; i++)
            {
                var fav = favorites[i];
                if (!fav.Enabled || string.IsNullOrEmpty(fav.Word)) continue;

                if (Match(ch, fav))
                {
                    if (fav.IsNG)
                    {
                        ch.IsNG = true;
                        break; // NG always wins
                    }

                    if (!ch.IsFavorite)
                    {
                        ch.IsFavorite = true;
                        ch.FavPriority = i;
                        ch.FavBackColor = ParseColor(fav.BackColor);
                        ch.FavTextColor = ParseColor(fav.TextColor);
                    }
                    // First match wins for color
                }
            }
        }
    }

    public bool Match(ChannelItem ch, FavoriteItem fav)
    {
        var text = BuildTargetText(ch, fav.TargetFields);
        if (string.IsNullOrEmpty(text)) return false;

        if (fav.IsRegex)
        {
            var regex = GetOrCreateRegex(fav.Word);
            return regex?.IsMatch(text) ?? false;
        }

        return text.Contains(fav.Word, StringComparison.OrdinalIgnoreCase);
    }

    // Returns list of channels that are newly favorited (Diff == New and IsFavorite)
    public List<ChannelItem> GetNewFavoriteChannels(IEnumerable<ChannelItem> channels) =>
        channels.Where(c => c.IsFavorite && !c.IsNG && c.Diff == ChannelDiff.New).ToList();

    private static string BuildTargetText(ChannelItem ch, FavoriteTargetFields fields)
    {
        var parts = new List<string>();
        if (fields.HasFlag(FavoriteTargetFields.ChannelName)) parts.Add(ch.ChannelName);
        if (fields.HasFlag(FavoriteTargetFields.Genre)) parts.Add(ch.Genre);
        if (fields.HasFlag(FavoriteTargetFields.Description)) parts.Add(ch.Description);
        if (fields.HasFlag(FavoriteTargetFields.Comment)) parts.Add(ch.Comment);
        if (fields.HasFlag(FavoriteTargetFields.ContactUrl)) parts.Add(ch.ContactUrl);
        if (fields.HasFlag(FavoriteTargetFields.YpName)) parts.Add(ch.YpName);
        if (fields.HasFlag(FavoriteTargetFields.ChannelType)) parts.Add(ch.ChannelType);
        if (fields.HasFlag(FavoriteTargetFields.TrackTitle)) parts.Add(ch.TrackTitle);
        if (fields.HasFlag(FavoriteTargetFields.TrackArtist)) parts.Add(ch.TrackArtist);
        return string.Join(" ", parts.Where(s => !string.IsNullOrEmpty(s)));
    }

    private Regex? GetOrCreateRegex(string pattern)
    {
        if (!_regexCache.TryGetValue(pattern, out var regex))
        {
            try
            {
                regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch
            {
                regex = null;
            }
            _regexCache[pattern] = regex;
        }
        return regex;
    }

    private static Windows.UI.Color? ParseColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return null;
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = Convert.ToByte(hex[0..2], 16);
                var g = Convert.ToByte(hex[2..4], 16);
                var b = Convert.ToByte(hex[4..6], 16);
                return Windows.UI.Color.FromArgb(255, r, g, b);
            }
        }
        catch { }
        return null;
    }
}
