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

    private static string BuildTargetText(ChannelItem ch, MatchTargetFields fields)
    {
        var parts = new List<string>();
        if (fields.HasFlag(MatchTargetFields.ChannelName)) parts.Add(ch.ChannelName);
        if (fields.HasFlag(MatchTargetFields.Genre))       parts.Add(ch.Genre);
        if (fields.HasFlag(MatchTargetFields.Description)) parts.Add(ch.Description);
        if (fields.HasFlag(MatchTargetFields.Comment))     parts.Add(ch.Comment);
        if (fields.HasFlag(MatchTargetFields.ContactUrl))  parts.Add(ch.ContactUrl);
        if (fields.HasFlag(MatchTargetFields.YpName))      parts.Add(ch.YpName);
        if (fields.HasFlag(MatchTargetFields.ChannelType)) parts.Add(ch.ChannelType);
        if (fields.HasFlag(MatchTargetFields.TrackTitle))  parts.Add(ch.TrackTitle);
        if (fields.HasFlag(MatchTargetFields.TrackArtist)) parts.Add(ch.TrackArtist);
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
