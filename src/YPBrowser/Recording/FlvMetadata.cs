namespace YPBrowser.Recording;

/// <summary>
/// 録画ファイル用の onMetaData を組み立てた結果。
/// duration / lasttimestamp / filesize は録画終了まで確定しないので、
/// 値（double 8 バイト）の位置を覚えておいて後から上書きする。
/// </summary>
public sealed class FlvMetadataBlock
{
    public required byte[] Payload { get; init; }

    /// <summary>ペイロード先頭からの相対位置。</summary>
    public required int DurationOffset { get; init; }

    public required int LastTimestampOffset { get; init; }

    public required int FileSizeOffset { get; init; }
}

public static class FlvMetadata
{
    /// <summary>
    /// 配信元の onMetaData から引き継ぐ値。ここに無いキーは捨てる。
    /// 特に keyframes（シーク用インデックス）は配信元のものが録画ファイルと対応しないため引き継がない。
    /// </summary>
    private static readonly string[] CopiedNumbers =
    [
        "width", "height", "videodatarate", "framerate", "videocodecid",
        "audiodatarate", "audiosamplerate", "audiosamplesize", "audiocodecid",
    ];

    private const string CopiedBoolean = "stereo";

    /// <summary>スクリプトタグのペイロード（タグヘッダは含まない）を作る。</summary>
    public static FlvMetadataBlock Build(IReadOnlyDictionary<string, object?> source)
    {
        var values = new List<(string Key, object Value)>();
        foreach (var key in CopiedNumbers)
        {
            if (source.TryGetValue(key, out var v) && v is double d && !double.IsNaN(d) && !double.IsInfinity(d))
                values.Add((key, d));
        }
        if (source.TryGetValue(CopiedBoolean, out var stereo) && stereo is bool b)
            values.Add((CopiedBoolean, b));

        // duration / lasttimestamp / filesize / canSeekToEnd / encoder を足した数
        var count = values.Count + 5;

        using var ms = new MemoryStream();
        Amf0.WriteString(ms, "onMetaData");
        Amf0.WriteEcmaArrayStart(ms, count);

        Amf0.WriteKey(ms, "duration");
        ms.WriteByte(0x00);
        var durationOffset = (int)ms.Position;
        Amf0.WriteDouble(ms, 0);

        foreach (var (key, value) in values)
        {
            Amf0.WriteKey(ms, key);
            switch (value)
            {
                case double d: Amf0.WriteNumber(ms, d); break;
                case bool flag: Amf0.WriteBoolean(ms, flag); break;
            }
        }

        Amf0.WriteKey(ms, "lasttimestamp");
        ms.WriteByte(0x00);
        var lastTimestampOffset = (int)ms.Position;
        Amf0.WriteDouble(ms, 0);

        Amf0.WriteKey(ms, "filesize");
        ms.WriteByte(0x00);
        var fileSizeOffset = (int)ms.Position;
        Amf0.WriteDouble(ms, 0);

        // シークインデックスを持たないので、末尾へのシークは保証しない
        Amf0.WriteKey(ms, "canSeekToEnd");
        Amf0.WriteBoolean(ms, false);

        Amf0.WriteKey(ms, "encoder");
        Amf0.WriteString(ms, "YPBrowser");

        Amf0.WriteObjectEnd(ms);

        return new FlvMetadataBlock
        {
            Payload = ms.ToArray(),
            DurationOffset = durationOffset,
            LastTimestampOffset = lastTimestampOffset,
            FileSizeOffset = fileSizeOffset,
        };
    }
}
