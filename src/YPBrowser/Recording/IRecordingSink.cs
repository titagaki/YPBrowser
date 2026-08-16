namespace YPBrowser.Recording;

/// <summary>
/// 録画データの書き込み先。コンテナ形式ごとの後始末をここに閉じ込める。
/// </summary>
public interface IRecordingSink : IAsyncDisposable
{
    /// <summary>新しい HTTP 応答の開始を通知する（初回接続とリトライ後の再接続で呼ぶ）。</summary>
    void BeginSegment();

    ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct);

    /// <summary>録画終了。メタデータの確定など、最後に一度だけ必要な処理を行う。</summary>
    ValueTask CompleteAsync(CancellationToken ct);
}
