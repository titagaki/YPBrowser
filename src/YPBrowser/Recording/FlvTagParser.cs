using System.Buffers.Binary;

namespace YPBrowser.Recording;

public readonly struct FlvTag
{
    public const byte TypeAudio = 8;
    public const byte TypeVideo = 9;
    public const byte TypeScript = 18;

    public byte Type { get; }

    /// <summary>タグヘッダの 24bit + 拡張 8bit を合成した値（ms）。</summary>
    public uint Timestamp { get; }

    public byte[] Data { get; }

    public FlvTag(byte type, uint timestamp, byte[] data)
    {
        Type = type;
        Timestamp = timestamp;
        Data = data;
    }

    /// <summary>AAC シーケンスヘッダ（デコーダ初期化情報）か。</summary>
    public bool IsAudioSequenceHeader =>
        Type == TypeAudio && Data.Length >= 2 && (Data[0] >> 4) == 10 && Data[1] == 0;

    /// <summary>AVC / HEVC シーケンスヘッダ（デコーダ初期化情報）か。</summary>
    public bool IsVideoSequenceHeader =>
        Type == TypeVideo && Data.Length >= 2 && ((Data[0] & 0x0F) is 7 or 12) && Data[1] == 0;
}

/// <summary>
/// バイト列を少しずつ受け取って FLV のヘッダとタグを切り出す。
/// HTTP は 81,920 バイト単位でぶつ切りに届きタグ境界とは無関係なので、
/// 揃うまで内部バッファに溜めて、揃ったぶんだけ返す。
/// </summary>
public sealed class FlvTagParser
{
    /// <summary>DataSize は 24bit なので理論上の上限は 16MB。これを超えたら同期外れとみなす。</summary>
    private const int MaxTagDataSize = 16 * 1024 * 1024;

    private byte[] _buffer = new byte[64 * 1024];
    private int _head;  // 消費済みの位置
    private int _tail;  // 有効データの終端

    /// <summary>FLV として辻褄が合わなくなった。以降は何も返さない。</summary>
    public bool IsBroken { get; private set; }

    /// <summary>未消費のバイト数。</summary>
    public int BufferedCount => _tail - _head;

    public void Reset()
    {
        _head = 0;
        _tail = 0;
        IsBroken = false;
    }

    public void Append(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.AsSpan(_tail));
        _tail += data.Length;
    }

    /// <summary>未消費のバイト列を取り出してバッファを空にする（素通しへ切り替えるときに使う）。</summary>
    public byte[] TakeBuffered()
    {
        var result = _buffer.AsSpan(_head, _tail - _head).ToArray();
        _head = 0;
        _tail = 0;
        return result;
    }

    /// <summary>
    /// FLV ヘッダと直後の PreviousTagSize0 を消費する。
    /// false かつ <see cref="IsBroken"/> なら FLV ではない。false だけならデータ待ち。
    /// </summary>
    public bool TryReadHeader(out byte typeFlags)
    {
        typeFlags = 0;
        if (IsBroken) return false;
        if (_tail - _head < 9) return false;

        var b = _buffer.AsSpan(_head);
        if (b[0] != (byte)'F' || b[1] != (byte)'L' || b[2] != (byte)'V')
        {
            IsBroken = true;
            return false;
        }

        typeFlags = b[4];
        var dataOffset = BinaryPrimitives.ReadUInt32BigEndian(b[5..9]);
        if (dataOffset < 9 || dataOffset > 1024)
        {
            IsBroken = true;
            return false;
        }

        // ヘッダ本体 + PreviousTagSize0 (4 バイト)
        if (_tail - _head < dataOffset + 4) return false;
        _head += (int)dataOffset + 4;
        return true;
    }

    /// <summary>
    /// タグを 1 個消費する。false かつ <see cref="IsBroken"/> なら同期外れ。false だけならデータ待ち。
    /// </summary>
    public bool TryReadTag(out FlvTag tag)
    {
        tag = default;
        if (IsBroken) return false;
        if (_tail - _head < 11) return false;

        var b = _buffer.AsSpan(_head);
        var type = b[0];
        int dataSize = (b[1] << 16) | (b[2] << 8) | b[3];
        if (dataSize > MaxTagDataSize)
        {
            IsBroken = true;
            return false;
        }

        var total = 11 + dataSize + 4;
        if (_tail - _head < total) return false;

        // 末尾の PreviousTagSize が合わなければ、どこかでズレている
        var prevTagSize = BinaryPrimitives.ReadUInt32BigEndian(b.Slice(11 + dataSize, 4));
        if (prevTagSize != 11 + dataSize)
        {
            IsBroken = true;
            return false;
        }

        var timestamp = ((uint)b[7] << 24) | ((uint)b[4] << 16) | ((uint)b[5] << 8) | b[6];
        var data = b.Slice(11, dataSize).ToArray();
        _head += total;
        tag = new FlvTag(type, timestamp, data);
        return true;
    }

    private void EnsureCapacity(int extra)
    {
        if (_tail + extra <= _buffer.Length) return;

        var length = _tail - _head;
        if (_head > 0)
        {
            Array.Copy(_buffer, _head, _buffer, 0, length);
            _head = 0;
            _tail = length;
            if (_tail + extra <= _buffer.Length) return;
        }

        var capacity = _buffer.Length;
        while (capacity < _tail + extra) capacity *= 2;
        Array.Resize(ref _buffer, capacity);
    }
}
