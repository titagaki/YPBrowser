using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface ITagMatchService
{
    /// <summary>
    /// 全チャンネルに対しルールを順に評価し、<see cref="ChannelItem.Tags"/> を差し替える。
    /// </summary>
    void ApplyTags(
        IEnumerable<ChannelItem> channels,
        IReadOnlyList<Rule> rules,
        IReadOnlyList<TagDefinition> tags);

    /// <summary>1件のルールが1件のチャンネルに一致するか。ルール編集画面のライブ評価にも使う。</summary>
    bool Evaluate(Rule rule, ChannelItem channel);

    /// <summary>ルールに一致するチャンネル（「該当を確認」用）。<see cref="Rule.Enabled"/> は見ない。</summary>
    List<ChannelItem> GetMatches(IEnumerable<ChannelItem> channels, Rule rule);

    /// <summary>
    /// 条件の正規表現が不正なら、そのエラーメッセージを返す。問題なければ null。
    /// </summary>
    string? ValidatePattern(RuleCondition condition);

    /// <summary>通知対象タグが付いた新着チャンネル。</summary>
    List<ChannelItem> GetChannelsToNotify(IEnumerable<ChannelItem> channels);
}
