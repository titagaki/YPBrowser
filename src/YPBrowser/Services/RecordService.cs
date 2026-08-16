using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Recording;
using YPBrowser.Settings;

namespace YPBrowser.Services;

public class RecordService : IRecordService
{
    private static readonly Dictionary<string, string> CodecExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MP3"]  = ".mp3",
        ["OGG"]  = ".ogg",
        ["OGV"]  = ".ogg",
        ["AAC"]  = ".aac",
        ["WMA"]  = ".wma",
        ["FLV"]  = ".flv",
        ["MKV"]  = ".mkv",
        ["WMV"]  = ".wmv",
        ["NSV"]  = ".nsv",
    };

    private const int MaxRetries = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RecordService> _logger;

    private readonly ConcurrentDictionary<string, (CancellationTokenSource Cts, RecordingEntry Entry)> _active = new();

    public IReadOnlyList<RecordingEntry> ActiveRecordings =>
        _active.Values.Select(v => v.Entry).ToList();

    public event EventHandler? RecordingsChanged;

    public RecordService(IHttpClientFactory httpClientFactory, ILogger<RecordService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public void StartRecording(ChannelItem channel, DownloaderSettings settings)
    {
        if (_active.ContainsKey(channel.Id))
        {
            _logger.LogWarning("Already recording channel {Id}", channel.Id);
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputDir = ResolveOutputDirectory(settings.OutputDirectory);
        var extension = ResolveExtension(channel.ChannelType);
        var filename = BuildFilename(channel, settings.FileNameTemplate, timestamp) + extension;
        var filePath = Path.Combine(outputDir, filename);

        var cts = new CancellationTokenSource();
        var entry = new RecordingEntry(
            channel.Id,
            channel.ChannelName,
            channel.GenreDescription,
            filePath);

        if (!_active.TryAdd(channel.Id, (cts, entry)))
        {
            cts.Dispose();
            return;
        }

        _logger.LogInformation("Recording started: {Channel} → {File}", channel.ChannelName, filePath);
        RecordingsChanged?.Invoke(this, EventArgs.Empty);

        _ = Task.Run(() => DownloadAsync(channel, filePath, extension, channel.Id, entry, cts.Token));
    }

    public void StopRecording(string channelId)
    {
        if (_active.TryRemove(channelId, out var pair))
        {
            _logger.LogInformation("Recording stopped for channel {Id}", channelId);
            pair.Cts.Cancel();
            pair.Cts.Dispose();
            RecordingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsRecording(string channelId) => _active.ContainsKey(channelId);

    private async Task DownloadAsync(
        ChannelItem channel, string filePath, string extension, string channelId,
        RecordingEntry entry, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("RecordService");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            await using var fileStream = new FileStream(
                filePath, FileMode.Create, FileAccess.Write, FileShare.Read,
                bufferSize: 81920, useAsync: true);

            await using var sink = CreateSink(extension, fileStream);

            try
            {
                for (int attempt = 0; attempt <= MaxRetries; attempt++)
                {
                    if (attempt > 0)
                    {
                        _logger.LogWarning("Retry {Attempt}/{Max} for channel {Id}", attempt, MaxRetries, channelId);
                        entry.RetryCount = attempt;
                        await Task.Delay(RetryDelay, ct);
                    }

                    try
                    {
                        var streamUrl = await ResolveStreamUrlAsync(client, channel.StreamUrl, ct);
                        if (attempt == 0)
                            _logger.LogInformation("Resolved stream URL: {Url}", streamUrl);

                        using var response = await client.GetAsync(
                            streamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                        response.EnsureSuccessStatusCode();

                        await using var networkStream = await response.Content.ReadAsStreamAsync(ct);
                        sink.BeginSegment();
                        await CopyWithProgressAsync(networkStream, sink, entry, ct);

                        _logger.LogInformation("Recording completed: {File}", filePath);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Recording cancelled: {File}", filePath);
                        return;
                    }
                    catch (Exception ex) when (attempt < MaxRetries)
                    {
                        _logger.LogWarning(ex, "Recording interrupted for channel {Id}, will retry in {Delay}s",
                            channelId, RetryDelay.TotalSeconds);
                    }
                }

                _logger.LogError("Recording failed after {Max} retries for channel {Id}", MaxRetries, channelId);
            }
            finally
            {
                // 停止でキャンセル済みでもメタデータは書き切る
                try
                {
                    await sink.CompleteAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to finalize {File}", filePath);
                }
            }
        }
        finally
        {
            if (_active.TryRemove(channelId, out var pair))
            {
                pair.Cts.Dispose();
                RecordingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private IRecordingSink CreateSink(string extension, Stream output) =>
        extension.Equals(".flv", StringComparison.OrdinalIgnoreCase)
            ? new FlvRecordingSink(output, _logger)
            : new RawRecordingSink(output);

    private static async Task CopyWithProgressAsync(
        Stream source, IRecordingSink sink, RecordingEntry entry, CancellationToken ct)
    {
        var buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            await sink.WriteAsync(buffer.AsMemory(0, read), ct);
            entry.AddBytes(read); // UI 更新は毎秒 Tick() で行う
        }
    }

    private async Task<string> ResolveStreamUrlAsync(HttpClient client, string initialUrl, CancellationToken ct)
    {
        using var response = await client.GetAsync(initialUrl, ct);
        if (!response.IsSuccessStatusCode)
            return initialUrl;

        var content = await response.Content.ReadAsStringAsync(ct);

        if (content.Contains("[playlist]", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = ParsePlsUrl(content);
            if (resolved != null && resolved != initialUrl)
            {
                _logger.LogDebug("PLS resolved: {Url}", resolved);
                return resolved;
            }
        }

        if (content.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase) ||
            content.StartsWith("#EXT-X-", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = ParseM3uUrl(content);
            if (resolved != null && resolved != initialUrl)
            {
                _logger.LogDebug("M3U resolved: {Url}", resolved);
                return resolved;
            }
        }

        return initialUrl;
    }

    private static string? ParsePlsUrl(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("File1=", StringComparison.OrdinalIgnoreCase))
            {
                var url = trimmed[6..].Trim();
                return string.IsNullOrEmpty(url) ? null : url;
            }
        }
        return null;
    }

    private static string? ParseM3uUrl(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('#') && !string.IsNullOrEmpty(trimmed))
                return trimmed;
        }
        return null;
    }

    private static string ResolveOutputDirectory(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
        }
        return Environment.ExpandEnvironmentVariables(raw);
    }

    private static string BuildFilename(ChannelItem channel, string template, string timestamp) =>
        template
            .Replace("{channelName}", SanitizeFilename(channel.ChannelName))
            .Replace("{timestamp}",   timestamp);

    private static string ResolveExtension(string? channelType) =>
        CodecExtensions.TryGetValue(channelType ?? "", out var e) ? e : ".ts";

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
    }
}
