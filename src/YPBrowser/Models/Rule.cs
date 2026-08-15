using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

/// <summary>条件が見る index.txt 側のフィールド。</summary>
public enum ConditionField
{
    ChannelName,
    /// <summary>ジャンル / 詳細 / コメントを連結したもの。</summary>
    Description,
    ContactUrl,
    /// <summary>
    /// index.txt の 11 番目（アーティスト欄）。UI では「Playing」と出す。
    /// 実際には配信中の曲名や <c>210.157.193.184 via Peercast Gateway</c> のような
    /// 配信経路が入ってくるので、アーティスト名という呼び方が実態に合わない。
    /// </summary>
    TrackArtist,
}

/// <summary>パターンの一致方式。</summary>
public enum ConditionMatchType
{
    Contains,
    Exact,
    Regex,
}

/// <summary>複数条件の集約方法。</summary>
public enum RuleCombinator
{
    /// <summary>すべて満たす。</summary>
    And,
    /// <summary>いずれか満たす。</summary>
    Or,
}

public partial class RuleCondition : ObservableObject
{
    [ObservableProperty] private ConditionField _field = ConditionField.Description;

    /// <summary>主な利用者は正規表現を書く層なので既定は <see cref="ConditionMatchType.Regex"/>。</summary>
    [ObservableProperty] private ConditionMatchType _matchType = ConditionMatchType.Regex;

    /// <summary>一致結果を反転する（「不一致」）。</summary>
    [ObservableProperty] private bool _negate;

    [ObservableProperty] private string _pattern = "";

    public RuleCondition Clone() => new()
    {
        Field = Field,
        MatchType = MatchType,
        Negate = Negate,
        Pattern = Pattern,
    };
}

/// <summary>
/// 条件に一致したチャンネルへタグを付けるだけのルール。色・通知・非表示は持たない。
/// </summary>
public partial class Rule : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _enabled = true;

    /// <summary>小さいほど先に評価する。</summary>
    [ObservableProperty] private int _order;

    [ObservableProperty] private RuleCombinator _combinator = RuleCombinator.And;

    public ObservableCollection<RuleCondition> Conditions { get; set; } = [];

    /// <summary>付与するタグ。タグ名はリネームされうるので必ず ID で参照する。</summary>
    public ObservableCollection<string> TagIds { get; set; } = [];

    /// <summary>メールフィルタの stop processing 相当。ここで評価を打ち切る。</summary>
    [ObservableProperty] private bool _stopProcessing;

    /// <summary>星ボタンが自動生成したルール。ルール一覧で通常のルールと区別する。</summary>
    [ObservableProperty] private bool _isAuto;

    /// <summary>編集用の複製。<paramref name="newId"/> が true なら別ルールとして複製する。</summary>
    public Rule Clone(bool newId = false) => new()
    {
        Id = newId ? Guid.NewGuid().ToString("N") : Id,
        Name = Name,
        Enabled = Enabled,
        Order = Order,
        Combinator = Combinator,
        Conditions = [.. Conditions.Select(c => c.Clone())],
        TagIds = [.. TagIds],
        StopProcessing = StopProcessing,
        IsAuto = IsAuto,
    };

    /// <summary>
    /// 星ボタン用のルール。チャンネル名の完全一致でお気に入りタグを付ける。
    /// `いまいch` のような名前を正規表現として評価すると誤爆するので、必ず <see cref="ConditionMatchType.Exact"/>。
    /// </summary>
    public static Rule CreateStarRule(string channelName) => new()
    {
        Name = channelName,
        IsAuto = true,
        Combinator = RuleCombinator.And,
        Conditions =
        [
            new RuleCondition
            {
                Field = ConditionField.ChannelName,
                MatchType = ConditionMatchType.Exact,
                Pattern = channelName,
            }
        ],
        TagIds = [TagDefinition.FavoriteId],
    };

    /// <summary>指定したチャンネル名に対応する星ルールかどうか。</summary>
    public bool IsStarRuleFor(string channelName) =>
        IsAuto
        && TagIds.Contains(TagDefinition.FavoriteId)
        && Conditions.Count == 1
        && Conditions[0].Field == ConditionField.ChannelName
        && Conditions[0].MatchType == ConditionMatchType.Exact
        && !Conditions[0].Negate
        && Conditions[0].Pattern == channelName;

    [JsonIgnore]
    public string ConditionSummary => Conditions.Count == 0
        ? "(条件なし)"
        : string.Join(Combinator == RuleCombinator.And ? " かつ " : " または ",
            Conditions.Select(c => c.Pattern));
}
