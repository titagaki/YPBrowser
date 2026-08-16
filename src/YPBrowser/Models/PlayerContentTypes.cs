namespace YPBrowser.Models;

/// <summary>
/// プレイヤーを割り当てるコンテンツタイプ。
/// YP の index.txt から来る <see cref="ChannelItem.ChannelType"/> と突き合わせる。
/// </summary>
public static class PlayerContentTypes
{
    /// <summary>
    /// 「その他」を表す値。どのタイプにも当てはまらなかったチャンネルを引き受ける。
    /// 空文字なのは、設定ファイルに書かれていない古い形からそのまま読めるようにするため。
    /// </summary>
    public const string Fallback = "";

    public const string FallbackLabel = "その他";

    /// <summary>名前で指定できるタイプ。並び順がそのまま一覧と選択肢の並び順になる。</summary>
    public static readonly string[] Known =
        ["FLV", "MKV", "WMV", "WMA", "OGG", "OGV", "MP3", "AAC", "NSV", "RAW"];

    /// <summary>編集ダイアログで選べるタイプ。「その他」は末尾。</summary>
    public static readonly string[] Selectable = [.. Known, Fallback];

    public static string Label(string contentType) =>
        string.IsNullOrEmpty(contentType) ? FallbackLabel : contentType;

    /// <summary>タイプが一致するか。YP から来る値は大小がまちまちなので区別しない。</summary>
    public static bool Matches(string contentType, string channelType) =>
        !string.IsNullOrEmpty(contentType)
        && string.Equals(contentType, channelType, StringComparison.OrdinalIgnoreCase);

    /// <summary>一覧に並べる順。「その他」は必ず末尾。</summary>
    public static int SortKey(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return int.MaxValue;

        var index = Array.FindIndex(Known,
            t => string.Equals(t, contentType, StringComparison.OrdinalIgnoreCase));

        // 設定ファイルを手で書き換えて未知のタイプを入れても、「その他」より前には置く
        return index >= 0 ? index : Known.Length;
    }
}
