using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Helpers;

/// <summary>チャンネルを再生するプレイヤーを選ぶ。</summary>
public static class PlayerSelection
{
    /// <summary>
    /// コンテンツタイプが一致するプレイヤーを使い、無ければ「その他」に落とす。
    /// どちらも無ければ <c>null</c>（呼び出し側が OS の既定ハンドラへ渡す）。
    /// </summary>
    public static PlayerSettings? For(IEnumerable<PlayerSettings> players, string? channelType)
    {
        var candidates = players as IReadOnlyList<PlayerSettings> ?? [.. players];

        return candidates.FirstOrDefault(p => PlayerContentTypes.Matches(p.ContentType, channelType ?? ""))
            ?? candidates.FirstOrDefault(p => string.IsNullOrEmpty(p.ContentType));
    }
}
