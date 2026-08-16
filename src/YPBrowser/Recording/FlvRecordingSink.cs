using System.Buffers.Binary;
using Microsoft.Extensions.Logging;

namespace YPBrowser.Recording;

/// <summary>FLV として辻褄が合わなくなり、そのまま書き続けられない。</summary>
public sealed class FlvStreamException : Exception
{
    public FlvStreamException(string message) : base(message) { }
}

/// <summary>
/// FLV を書き換えながら保存する。素通しだと次の 2 点で単体再生できないファイルになる。
///
/// 1. タグのタイムスタンプは配信開始からの絶対時間。1 時間番組の最後の 10 分だけ録画すると
///    先頭タグが 50 分の位置から始まり、プレイヤーは「50 分の空白 + 10 分」と解釈する。
///    → 録画した最初のメディアタグを 0 として全タグから引く。
/// 2. 切断→再接続のたびに配信元は FLV ヘッダを再送する。素通しするとファイルの途中に
///    別ファイルの先頭が刺さり、多くのプレイヤーはそこで再生を打ち切る。
///    → 2 回目以降のヘッダは捨て、時間軸は前のセグメントの続きから繋ぐ。
///
/// あわせて onMetaData も差し替える。配信元の duration は番組全体の長さで録画長ではないため。
/// </summary>
public sealed class FlvRecordingSink : IRecordingSink
{
    /// <summary>duration などを書き直す間隔（ストリーム時間）。強制終了されても直近まで正しく残る。</summary>
    private const long MetadataRefreshIntervalMs = 10_000;

    private readonly Stream _out;
    private readonly ILogger _logger;
    private readonly FlvTagParser _parser = new();
    private readonly Dictionary<string, object?> _sourceMetadata = new(StringComparer.Ordinal);

    private bool _fileHeaderWritten;
    private bool _metadataWritten;
    private bool _completed;

    /// <summary>FLV として解釈できなかった場合の逃げ道。以降は素通しする。</summary>
    private bool _passthrough;

    private long _durationPosition;
    private long _lastTimestampPosition;
    private long _fileSizePosition;

    private bool _segmentNeedsHeader;
    private long _segmentBaseIn = -1;   // このセグメントの最初のメディアタグの入力 ts
    private long _segmentOutBase;       // このセグメントを出力上のどの時刻から始めるか

    private long _maxOutTs;
    private long _lastRefreshedTs;
    private long _bytesWritten;

    private byte[]? _audioSequenceHeader;
    private byte[]? _videoSequenceHeader;

    public FlvRecordingSink(Stream output, ILogger logger)
    {
        _out = output;
        _logger = logger;
    }

    public void BeginSegment()
    {
        _parser.Reset();
        _segmentNeedsHeader = true;
        _segmentBaseIn = -1;
        _segmentOutBase = _maxOutTs;
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (_passthrough)
        {
            await _out.WriteAsync(data, ct);
            return;
        }

        _parser.Append(data.Span);
        await PumpAsync(ct);
    }

    public async ValueTask CompleteAsync(CancellationToken ct)
    {
        if (!_completed)
        {
            _completed = true;
            if (!_passthrough) await RefreshMetadataAsync(ct);
        }
        await _out.FlushAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        // 本来の失敗は CompleteAsync の呼び出し側が扱う。破棄で例外を投げ直さない
        try { await CompleteAsync(CancellationToken.None); }
        catch (Exception ex) { _logger.LogWarning(ex, "FLV の後始末に失敗しました"); }
    }

    private async ValueTask PumpAsync(CancellationToken ct)
    {
        if (_segmentNeedsHeader)
        {
            if (!_parser.TryReadHeader(out var typeFlags))
            {
                if (!_parser.IsBroken) return; // まだヘッダが揃っていない

                if (!_fileHeaderWritten)
                {
                    // FLV ではなかった。ここで捨てるより、素通しで残したほうがまだ使える
                    _logger.LogWarning("FLV ヘッダが見つからないため、書き換えずそのまま保存します");
                    _passthrough = true;
                    await _out.WriteAsync(_parser.TakeBuffered(), ct);
                    return;
                }

                throw new FlvStreamException("再接続後の応答が FLV ヘッダで始まっていない");
            }

            _segmentNeedsHeader = false;
            if (!_fileHeaderWritten) await WriteFileHeaderAsync(typeFlags, ct);
        }

        while (_parser.TryReadTag(out var tag))
            await HandleTagAsync(tag, ct);

        if (_parser.IsBroken)
            throw new FlvStreamException("FLV タグの同期が外れた");
    }

    private async ValueTask HandleTagAsync(FlvTag tag, CancellationToken ct)
    {
        var isConfig = false;

        switch (tag.Type)
        {
            case FlvTag.TypeScript:
                if (Amf0.TryReadScriptData(tag.Data, out var name, out var properties) && name == "onMetaData")
                {
                    // 配信元のメタデータは値だけ拝借して、タグ自体は自前のものに差し替える
                    if (!_metadataWritten)
                    {
                        foreach (var (key, value) in properties)
                            _sourceMetadata[key] = value;
                        await WriteMetadataAsync(ct);
                    }
                    return;
                }
                isConfig = true;
                break;

            case FlvTag.TypeAudio:
                if (tag.IsAudioSequenceHeader)
                {
                    isConfig = true;
                    // 再接続のたびに同じものが送られてくる。中身が変わったときだけ書く
                    if (_audioSequenceHeader.AsSpan().SequenceEqual(tag.Data)) return;
                    _audioSequenceHeader = tag.Data;
                }
                break;

            case FlvTag.TypeVideo:
                if (tag.IsVideoSequenceHeader)
                {
                    isConfig = true;
                    if (_videoSequenceHeader.AsSpan().SequenceEqual(tag.Data)) return;
                    _videoSequenceHeader = tag.Data;
                }
                break;

            default:
                return; // 未知のタグ型は捨てる
        }

        // メタデータは必ず先頭に置く。配信元が onMetaData を送ってこない場合はここで自前のものを書く
        if (!_metadataWritten) await WriteMetadataAsync(ct);

        await WriteTagAsync(tag.Type, ResolveTimestamp(tag, isConfig), tag.Data, ct);
    }

    /// <summary>
    /// シーケンスヘッダやスクリプトタグは配信開始時の ts（多くは 0）を持ったまま送られてくるので、
    /// これらを基準にすると引き算が効かなくなる。基準はメディアタグだけで決める。
    /// </summary>
    private long ResolveTimestamp(FlvTag tag, bool isConfig)
    {
        if (isConfig) return _segmentOutBase;

        if (_segmentBaseIn < 0) _segmentBaseIn = tag.Timestamp;
        var relative = tag.Timestamp - _segmentBaseIn;
        if (relative < 0) relative = 0;
        return _segmentOutBase + relative;
    }

    private async ValueTask WriteFileHeaderAsync(byte typeFlags, CancellationToken ct)
    {
        var header = new byte[13];
        header[0] = (byte)'F';
        header[1] = (byte)'L';
        header[2] = (byte)'V';
        header[3] = 1;
        header[4] = typeFlags;      // 音声/映像の有無は配信元のものをそのまま引き継ぐ
        header[8] = 9;              // DataOffset
        // header[9..13] = PreviousTagSize0 = 0
        await _out.WriteAsync(header, ct);
        _bytesWritten += header.Length;
        _fileHeaderWritten = true;
    }

    private async ValueTask WriteMetadataAsync(CancellationToken ct)
    {
        var block = FlvMetadata.Build(_sourceMetadata);
        var payloadPosition = _bytesWritten + 11; // タグヘッダのぶん

        _durationPosition      = payloadPosition + block.DurationOffset;
        _lastTimestampPosition = payloadPosition + block.LastTimestampOffset;
        _fileSizePosition      = payloadPosition + block.FileSizeOffset;
        _metadataWritten = true;

        await WriteTagAsync(FlvTag.TypeScript, 0, block.Payload, ct);
    }

    private async ValueTask WriteTagAsync(byte type, long timestamp, byte[] payload, CancellationToken ct)
    {
        var ts = (uint)timestamp;
        var header = new byte[11];
        header[0] = type;
        header[1] = (byte)(payload.Length >> 16);
        header[2] = (byte)(payload.Length >> 8);
        header[3] = (byte)payload.Length;
        header[4] = (byte)(ts >> 16);
        header[5] = (byte)(ts >> 8);
        header[6] = (byte)ts;
        header[7] = (byte)(ts >> 24);
        // header[8..11] = StreamID = 0

        var trailer = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(trailer, (uint)(11 + payload.Length));

        await _out.WriteAsync(header, ct);
        await _out.WriteAsync(payload, ct);
        await _out.WriteAsync(trailer, ct);
        _bytesWritten += header.Length + payload.Length + trailer.Length;

        if (timestamp > _maxOutTs) _maxOutTs = timestamp;

        if (_maxOutTs - _lastRefreshedTs >= MetadataRefreshIntervalMs)
        {
            _lastRefreshedTs = _maxOutTs;
            await RefreshMetadataAsync(ct);
        }
    }

    /// <summary>録画長・ファイルサイズを onMetaData に書き戻す。</summary>
    private async ValueTask RefreshMetadataAsync(CancellationToken ct)
    {
        if (!_metadataWritten) return;

        var resume = _out.Position;
        var seconds = _maxOutTs / 1000.0;
        await WriteDoubleAtAsync(_durationPosition, seconds, ct);
        await WriteDoubleAtAsync(_lastTimestampPosition, seconds, ct);
        await WriteDoubleAtAsync(_fileSizePosition, _bytesWritten, ct);
        _out.Seek(resume, SeekOrigin.Begin);
    }

    private async ValueTask WriteDoubleAtAsync(long position, double value, CancellationToken ct)
    {
        var buffer = new byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
        _out.Seek(position, SeekOrigin.Begin);
        await _out.WriteAsync(buffer, ct);
    }
}
