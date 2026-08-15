namespace YPBrowser.Models;

/// <summary>コンボボックス1項目。</summary>
public record EnumOption<T>(T Value, string Label);

/// <summary>ルール編集画面のコンボボックスの選択肢。XAML から <c>x:Static</c> で参照する。</summary>
public static class RuleOptions
{
    public static IReadOnlyList<EnumOption<ConditionField>> Fields { get; } =
    [
        new(ConditionField.ChannelName, "チャンネル名"),
        new(ConditionField.Description, "ジャンル/詳細/コメント"),
        new(ConditionField.ContactUrl, "コンタクトURL"),
        new(ConditionField.TrackArtist, "Playing"),
    ];

    public static IReadOnlyList<EnumOption<ConditionMatchType>> MatchTypes { get; } =
    [
        new(ConditionMatchType.Contains, "部分一致"),
        new(ConditionMatchType.Exact, "完全一致"),
        new(ConditionMatchType.Regex, "正規表現"),
    ];

    public static IReadOnlyList<EnumOption<bool>> Negations { get; } =
    [
        new(false, "一致"),
        new(true, "不一致"),
    ];

    public static IReadOnlyList<EnumOption<RuleCombinator>> Combinators { get; } =
    [
        new(RuleCombinator.And, "すべて満たす"),
        new(RuleCombinator.Or, "いずれか満たす"),
    ];

    public static IReadOnlyList<EnumOption<TagDefaultAction>> DefaultActions { get; } =
    [
        new(TagDefaultAction.Normal, "通常表示"),
        new(TagDefaultAction.Highlight, "強調して上へ"),
        new(TagDefaultAction.Hidden, "一覧から隠す"),
    ];
}
