namespace YPBrowser.Models;

/// <summary>よく使うプレイヤーの引数 1 件。</summary>
/// <param name="ExecutableName">この引数を想定している実行ファイル名</param>
/// <param name="ArgumentTemplate">引数</param>
public record PlayerPreset(string ExecutableName, string ArgumentTemplate)
{
    /// <summary>メニューに出す 1 行。コマンドラインそのままの見た目にする。</summary>
    public string Display => $"{ExecutableName} {ArgumentTemplate}";
}

/// <summary>
/// 「設定例」。プレイヤーごとに引数の書き方が違い、毎回手で打つのは間違えやすいので、
/// 代表的なものを選ぶだけで入るようにしてある。
/// </summary>
public static class PlayerPresets
{
    public static readonly IReadOnlyList<PlayerPreset> All =
    [
        new("PCRPlayer.exe", "\"{stream}\" \"{channelname}\" \"{contact}\""),
        new("pcfp.exe",      "\"{stream}\" \"{channelname}\" \"{direct}\""),
        new("pcwmp.exe",     "\"{stream}\" \"{channelname}\" \"{direct}\""),
        // nkp は自前の記法 <stream/> を使う。置換の対象外なのでそのまま渡る
        new("nkp.exe",       "\"<stream/>\" --title=\"{channelname} - {description}\""),
    ];

    /// <summary>
    /// 実行ファイルのパスから、対応する例を探す。
    /// 参照ボタンで選んだ直後に引数を埋めるために使う。
    /// </summary>
    public static PlayerPreset? ForExecutable(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var name = Path.GetFileName(path);
        return All.FirstOrDefault(p => string.Equals(p.ExecutableName, name, StringComparison.OrdinalIgnoreCase));
    }
}
