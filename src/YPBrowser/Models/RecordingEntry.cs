using CommunityToolkit.Mvvm.ComponentModel;

namespace YPBrowser.Models;

public partial class RecordingEntry : ObservableObject
{
    public string ChannelId { get; }
    public string ChannelName { get; }
    public string ChannelDetail { get; }
    public string FilePath { get; }
    public DateTime StartedAt { get; }

    // バックグラウンドスレッドから加算。Tick() で毎秒 BytesDownloaded に反映する
    private long _pendingBytes;
    private DateTime? _finishedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private bool _isActive = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    private long _bytesDownloaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RetryCountDisplay))]
    private int _retryCount;

    public string DisplayName => IsActive ? ChannelName : $"{ChannelName}（終了）";
    public string RetryCountDisplay => RetryCount == 0 ? "" : RetryCount.ToString();

    public string StatusDisplay
    {
        get
        {
            var end = _finishedAt ?? DateTime.Now;
            var e = end - StartedAt;
            var kb = BytesDownloaded / 1024;
            return $"({(int)e.TotalHours}:{e.Minutes:D2}:{e.Seconds:D2}) {kb:#,##0}KB";
        }
    }

    partial void OnIsActiveChanged(bool value)
    {
        if (!value)
            _finishedAt = DateTime.Now;
    }

    /// <summary>バックグラウンドスレッドから安全に加算</summary>
    public void AddBytes(int n) => Interlocked.Add(ref _pendingBytes, n);

    /// <summary>毎秒 DispatcherTimer から呼ぶ。KB 数字更新 + 経過時間更新</summary>
    public void Tick()
    {
        if (!IsActive) return;
        var current = Interlocked.Read(ref _pendingBytes);
        if (current != BytesDownloaded)
            BytesDownloaded = current; // [NotifyPropertyChangedFor] が StatusDisplay も通知
        else
            OnPropertyChanged(nameof(StatusDisplay)); // 経過時間だけ更新
    }

    public RecordingEntry(string channelId, string channelName, string channelDetail, string filePath)
    {
        ChannelId = channelId;
        ChannelName = channelName;
        ChannelDetail = channelDetail;
        FilePath = filePath;
        StartedAt = DateTime.Now;
    }
}
