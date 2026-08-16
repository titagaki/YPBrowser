namespace YPBrowser.Recording;

/// <summary>
/// 受け取ったバイト列をそのまま書く。自己同期する形式（MP3/AAC/OGG/MPEG-TS）向け。
/// </summary>
public sealed class RawRecordingSink : IRecordingSink
{
    private readonly Stream _out;

    public RawRecordingSink(Stream output) => _out = output;

    public void BeginSegment() { }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct) =>
        _out.WriteAsync(data, ct);

    public ValueTask CompleteAsync(CancellationToken ct) => new(_out.FlushAsync(ct));

    public async ValueTask DisposeAsync()
    {
        // 本来の失敗は CompleteAsync の呼び出し側が扱う。破棄で例外を投げ直さない
        try { await CompleteAsync(CancellationToken.None); }
        catch { }
    }
}
