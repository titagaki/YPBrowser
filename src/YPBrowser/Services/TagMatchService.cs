using System.Text.RegularExpressions;
using YPBrowser.Abstractions;
using YPBrowser.Models;

namespace YPBrowser.Services;

/// <summary>
/// ルールを評価してチャンネルにタグを付ける。表示（色・通知・非表示）は決めない。
/// </summary>
public class TagMatchService : ITagMatchService
{
    // 判定は毎ポーリングごとに 全チャンネル × 全ルール で走るので、
    // コンパイル済み正規表現をパターン文字列でキャッシュする。
    // 不正なパターンは null としてキャッシュし、そのルールは常に不一致になる。
    private readonly Dictionary<string, Regex?> _regexCache = [];

    public void ApplyTags(
        IEnumerable<ChannelItem> channels,
        IReadOnlyList<Rule> rules,
        IReadOnlyList<TagDefinition> tags)
    {
        var tagById = new Dictionary<string, TagDefinition>();
        // タグの並び順 = 色を決めるときの優先順。後で並べ直せるよう index を覚えておく。
        var orderById = new Dictionary<string, int>();
        for (int i = 0; i < tags.Count; i++)
        {
            tagById[tags[i].Id] = tags[i];
            orderById[tags[i].Id] = i;
        }

        var activeRules = rules
            .Where(r => r.Enabled)
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var channel in channels)
        {
            var matched = new List<string>();

            foreach (var rule in activeRules)
            {
                if (!Evaluate(rule, channel)) continue;

                foreach (var tagId in rule.TagIds)
                {
                    if (!matched.Contains(tagId)) matched.Add(tagId);
                }

                if (rule.StopProcessing) break;
            }

            channel.Tags = matched.Count == 0
                ? []
                : [.. matched
                    .Where(tagById.ContainsKey)
                    .OrderBy(id => orderById[id])
                    .Select(id => tagById[id])];
        }
    }

    public bool Evaluate(Rule rule, ChannelItem channel)
    {
        if (rule.Conditions.Count == 0) return false;

        return rule.Combinator == RuleCombinator.And
            ? rule.Conditions.All(c => EvaluateCondition(c, channel))
            : rule.Conditions.Any(c => EvaluateCondition(c, channel));
    }

    public List<ChannelItem> GetMatches(IEnumerable<ChannelItem> channels, Rule rule) =>
        channels.Where(c => Evaluate(rule, c)).ToList();

    public string? ValidatePattern(RuleCondition condition)
    {
        if (condition.MatchType != ConditionMatchType.Regex) return null;
        if (string.IsNullOrEmpty(condition.Pattern)) return null;

        try
        {
            _ = new Regex(condition.Pattern);
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }

    public List<ChannelItem> GetChannelsToNotify(IEnumerable<ChannelItem> channels) =>
        channels
            .Where(c => c.IsNew && !c.IsHidden && c.Tags.Any(t => t.Notify))
            .ToList();

    private bool EvaluateCondition(RuleCondition condition, ChannelItem channel)
    {
        var result = MatchesPattern(condition, GetFieldText(channel, condition.Field));
        return condition.Negate ? !result : result;
    }

    private bool MatchesPattern(RuleCondition condition, string text)
    {
        if (string.IsNullOrEmpty(condition.Pattern)) return false;

        return condition.MatchType switch
        {
            ConditionMatchType.Exact =>
                string.Equals(text, condition.Pattern, StringComparison.OrdinalIgnoreCase),
            ConditionMatchType.Contains =>
                text.Contains(condition.Pattern, StringComparison.OrdinalIgnoreCase),
            ConditionMatchType.Regex =>
                GetOrCreateRegex(condition.Pattern)?.IsMatch(text) ?? false,
            _ => false,
        };
    }

    private static string GetFieldText(ChannelItem ch, ConditionField field) => field switch
    {
        ConditionField.ChannelName => ch.ChannelName,
        // index.txt の ジャンル / 詳細 / コメント を連結したもの
        ConditionField.Description => string.Join(" ",
            new[] { ch.Genre, ch.Description, ch.Comment }.Where(s => !string.IsNullOrEmpty(s))),
        ConditionField.ContactUrl => ch.ContactUrl,
        ConditionField.TrackArtist => ch.TrackArtist,
        _ => "",
    };

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
