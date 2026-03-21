using System.Text.RegularExpressions;
using YPBrowser.Abstractions;
using YPBrowser.Models;

namespace YPBrowser.Services;

public class AutoDownloadMatchService : IAutoDownloadMatchService
{
    private readonly Dictionary<string, Regex?> _regexCache = [];

    public List<ChannelItem> GetChannelsToAutoDownload(
        IEnumerable<ChannelItem> channels,
        IReadOnlyList<AutoDownloadRuleItem> rules)
    {
        return channels
            .Where(ch => ch.Diff == ChannelDiff.New && rules.Any(r => Match(ch, r)))
            .ToList();
    }

    private bool Match(ChannelItem ch, AutoDownloadRuleItem rule)
    {
        if (!rule.Enabled || string.IsNullOrEmpty(rule.Word)) return false;

        var text = BuildTargetText(ch, rule.TargetFields);
        if (string.IsNullOrEmpty(text)) return false;

        if (rule.IsRegex)
        {
            var regex = GetOrCreateRegex(rule.Word);
            return regex?.IsMatch(text) ?? false;
        }

        return text.Contains(rule.Word, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTargetText(ChannelItem ch, FavoriteTargetFields fields)
    {
        var parts = new List<string>();
        if (fields.HasFlag(FavoriteTargetFields.ChannelName)) parts.Add(ch.ChannelName);
        if (fields.HasFlag(FavoriteTargetFields.Genre))       parts.Add(ch.Genre);
        if (fields.HasFlag(FavoriteTargetFields.Description)) parts.Add(ch.Description);
        if (fields.HasFlag(FavoriteTargetFields.Comment))     parts.Add(ch.Comment);
        if (fields.HasFlag(FavoriteTargetFields.ContactUrl))  parts.Add(ch.ContactUrl);
        if (fields.HasFlag(FavoriteTargetFields.YpName))      parts.Add(ch.YpName);
        if (fields.HasFlag(FavoriteTargetFields.ChannelType)) parts.Add(ch.ChannelType);
        if (fields.HasFlag(FavoriteTargetFields.TrackTitle))  parts.Add(ch.TrackTitle);
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
}
