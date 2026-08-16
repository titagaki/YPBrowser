using System.Buffers.Binary;
using System.Text;

namespace YPBrowser.Recording;

/// <summary>
/// FLV のスクリプトタグ（onMetaData）に使われる AMF0 の読み書き。
/// 必要なのは数値・真偽・文字列の取り出しと、固定した組み合わせの書き出しだけ。
/// </summary>
public static class Amf0
{
    private const byte MarkerNumber      = 0x00;
    private const byte MarkerBoolean     = 0x01;
    private const byte MarkerString      = 0x02;
    private const byte MarkerObject      = 0x03;
    private const byte MarkerNull        = 0x05;
    private const byte MarkerUndefined   = 0x06;
    private const byte MarkerEcmaArray   = 0x08;
    private const byte MarkerObjectEnd   = 0x09;
    private const byte MarkerStrictArray = 0x0A;
    private const byte MarkerDate        = 0x0B;
    private const byte MarkerLongString  = 0x0C;

    private const int MaxDepth = 16;

    /// <summary>
    /// スクリプトタグのペイロードを「名前 + プロパティ」として読む。
    /// 途中で壊れていても、そこまでに読めたプロパティは返す。
    /// </summary>
    public static bool TryReadScriptData(
        ReadOnlySpan<byte> payload, out string name, out Dictionary<string, object?> properties)
    {
        name = "";
        properties = new Dictionary<string, object?>(StringComparer.Ordinal);

        var pos = 0;
        if (!TryReadValue(payload, ref pos, 0, out var nameValue) || nameValue is not string s)
            return false;

        name = s;

        // 本体が壊れていても名前が取れていれば成功扱い（プロパティは読めたぶんだけ）
        TryReadValue(payload, ref pos, 0, out var body);
        if (body is Dictionary<string, object?> dict)
            properties = dict;

        return true;
    }

    private static bool TryReadValue(ReadOnlySpan<byte> b, ref int pos, int depth, out object? value)
    {
        value = null;
        if (depth > MaxDepth || pos >= b.Length) return false;

        var marker = b[pos++];
        switch (marker)
        {
            case MarkerNumber:
                if (pos + 8 > b.Length) return false;
                value = BinaryPrimitives.ReadDoubleBigEndian(b.Slice(pos, 8));
                pos += 8;
                return true;

            case MarkerBoolean:
                if (pos >= b.Length) return false;
                value = b[pos++] != 0;
                return true;

            case MarkerString:
                return TryReadString(b, ref pos, out value);

            case MarkerObject:
                return TryReadProperties(b, ref pos, depth, out value);

            case MarkerEcmaArray:
                // 要素数は当てにならないので読み飛ばし、終端マーカーまで読む
                if (pos + 4 > b.Length) return false;
                pos += 4;
                return TryReadProperties(b, ref pos, depth, out value);

            case MarkerNull:
            case MarkerUndefined:
                return true;

            case MarkerStrictArray:
            {
                if (pos + 4 > b.Length) return false;
                var count = BinaryPrimitives.ReadUInt32BigEndian(b.Slice(pos, 4));
                pos += 4;
                if (count > b.Length - pos) return false; // 1 要素 1 バイト未満はあり得ない
                var items = new List<object?>((int)count);
                for (var i = 0u; i < count; i++)
                {
                    if (!TryReadValue(b, ref pos, depth + 1, out var item)) return false;
                    items.Add(item);
                }
                value = items;
                return true;
            }

            case MarkerDate:
                if (pos + 10 > b.Length) return false;
                value = BinaryPrimitives.ReadDoubleBigEndian(b.Slice(pos, 8));
                pos += 10; // double + タイムゾーン (S16)
                return true;

            case MarkerLongString:
            {
                if (pos + 4 > b.Length) return false;
                var length = BinaryPrimitives.ReadUInt32BigEndian(b.Slice(pos, 4));
                pos += 4;
                if (length > b.Length - pos) return false;
                value = Encoding.UTF8.GetString(b.Slice(pos, (int)length));
                pos += (int)length;
                return true;
            }

            default:
                return false; // 未知のマーカーは長さが分からないので追えない
        }
    }

    private static bool TryReadString(ReadOnlySpan<byte> b, ref int pos, out object? value)
    {
        value = null;
        if (pos + 2 > b.Length) return false;
        int length = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(pos, 2));
        pos += 2;
        if (pos + length > b.Length) return false;
        value = Encoding.UTF8.GetString(b.Slice(pos, length));
        pos += length;
        return true;
    }

    private static bool TryReadProperties(ReadOnlySpan<byte> b, ref int pos, int depth, out object? value)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        value = dict;

        while (true)
        {
            if (pos + 2 > b.Length) return false;
            int keyLength = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(pos, 2));
            if (keyLength == 0)
            {
                pos += 2;
                if (pos < b.Length && b[pos] == MarkerObjectEnd) pos++;
                return true;
            }

            pos += 2;
            if (pos + keyLength > b.Length) return false;
            var key = Encoding.UTF8.GetString(b.Slice(pos, keyLength));
            pos += keyLength;

            if (!TryReadValue(b, ref pos, depth + 1, out var propertyValue)) return false;
            dict[key] = propertyValue;
        }
    }

    // ---- 書き出し ----

    public static void WriteString(Stream s, string value)
    {
        s.WriteByte(MarkerString);
        WriteRawString(s, value);
    }

    public static void WriteKey(Stream s, string key) => WriteRawString(s, key);

    public static void WriteNumber(Stream s, double value)
    {
        s.WriteByte(MarkerNumber);
        WriteDouble(s, value);
    }

    public static void WriteBoolean(Stream s, bool value)
    {
        s.WriteByte(MarkerBoolean);
        s.WriteByte(value ? (byte)1 : (byte)0);
    }

    public static void WriteEcmaArrayStart(Stream s, int count)
    {
        s.WriteByte(MarkerEcmaArray);
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)count);
        s.Write(buffer);
    }

    public static void WriteObjectEnd(Stream s)
    {
        s.WriteByte(0);
        s.WriteByte(0);
        s.WriteByte(MarkerObjectEnd);
    }

    public static void WriteDouble(Stream s, double value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
        s.Write(buffer);
    }

    private static void WriteRawString(Stream s, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(length, (ushort)bytes.Length);
        s.Write(length);
        s.Write(bytes);
    }
}
