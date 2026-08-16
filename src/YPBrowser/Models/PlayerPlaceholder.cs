using System.Text.RegularExpressions;

namespace YPBrowser.Models;

/// <summary>引数テンプレートで使える置換子 1 件。</summary>
/// <param name="Token">波かっこ込みの表記（<c>{stream}</c>）</param>
/// <param name="Label">編集ダイアログに出す説明</param>
/// <param name="Resolve">チャンネルから実際の値を取り出す</param>
public record PlayerPlaceholder(string Token, string Label, Func<ChannelItem, string> Resolve);

/// <summary>
/// プレイヤーの引数テンプレートの置換。
/// 何が使えるかを 1 か所にまとめてあり、編集ダイアログの一覧も起動時の置換もここを見る
/// （別々に持つと、片方だけ増えて説明と実際がずれる）。
/// </summary>
public static partial class PlayerPlaceholders
{
    /// <summary>編集ダイアログに出す順。</summary>
    public static readonly IReadOnlyList<PlayerPlaceholder> All =
    [
        new("{stream}",      "ストリームURL",     c => c.StreamUrl),
        new("{channelname}", "チャンネル名",       c => c.ChannelName),
        new("{contact}",     "コンタクトURL",     c => c.ContactUrl),
        new("{genre}",       "ジャンル",           c => c.Genre),
        new("{description}", "詳細",               c => c.Description),
        new("{comment}",     "コメント",           c => c.Comment),
        new("{contenttype}", "コンテンツタイプ",   c => c.ChannelType),
        new("{direct}",      "ダイレクトの有無",   c => c.IsDirect ? "1" : "0"),
    ];

    /// <summary>
    /// 波かっこを外した名前で引く表。
    /// <c>url</c> は <c>{stream}</c> の旧名。設定は読み込み時に書き換わるが、
    /// 手で書いた設定ファイルが黙って壊れないように受け付けたままにしてある。
    /// </summary>
    private static readonly Dictionary<string, PlayerPlaceholder> ByName = BuildIndex();

    private static Dictionary<string, PlayerPlaceholder> BuildIndex()
    {
        var index = All.ToDictionary(p => p.Token.Trim('{', '}'), StringComparer.OrdinalIgnoreCase);
        index["url"] = All[0];
        return index;
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex TokenPattern();

    /// <summary>
    /// テンプレートを実際の引数へ展開する。
    /// 知らない語は書かれたまま残す。プレイヤー自身が波かっこを使う記法を持っていることがあり、
    /// 空文字に潰すと引数の数が変わってしまうため。
    /// </summary>
    public static string Expand(string? template, ChannelItem channel) =>
        TokenPattern().Replace(template ?? "", match =>
            ByName.TryGetValue(match.Groups[1].Value, out var placeholder)
                ? placeholder.Resolve(channel)
                : match.Value);
}
