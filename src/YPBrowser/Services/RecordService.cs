using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;
using YPBrowser.Models;
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

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RecordService> _logger;

    // channelId → CancellationTokenSource for active recordings
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _active = new();

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

        var cts = new CancellationTokenSource();
        if (!_active.TryAdd(channel.Id, cts))
        {
            cts.Dispose();
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputDir = ResolveOutputDirectory(settings.OutputDirectory);
        var filename = BuildFilename(channel, settings.FileNameTemplate, timestamp);
        var filePath = Path.Combine(outputDir, filename);

        _logger.LogInformation("Recording started: {Channel} → {File}", channel.ChannelName, filePath);

        _ = Task.Run(() => DownloadAsync(channel.StreamUrl, filePath, channel.Id, cts.Token));
    }

    public void StopRecording(string channelId)
    {
        if (_active.TryRemove(channelId, out var cts))
        {
            _logger.LogInformation("Recording stopped for channel {Id}", channelId);
            cts.Cancel();
            cts.Dispose();
        }
    }

    public bool IsRecording(string channelId) => _active.ContainsKey(channelId);

    private async Task DownloadAsync(string url, string filePath, string channelId, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var client = _httpClientFactory.CreateClient("RecordService");

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(
                filePath, FileMode.Create, FileAccess.Write, FileShare.Read,
                bufferSize: 81920, useAsync: true);

            await response.Content.CopyToAsync(fileStream, ct);

            _logger.LogInformation("Recording completed: {File}", filePath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Recording cancelled: {File}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recording failed for channel {Id}", channelId);
        }
        finally
        {
            // Clean up CTS if it's still in the dict (not removed by StopRecording)
            if (_active.TryRemove(channelId, out var cts))
                cts.Dispose();
        }
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

    private static string BuildFilename(ChannelItem channel, string template, string timestamp)
    {
        var name = template
            .Replace("{channelName}", SanitizeFilename(channel.ChannelName))
            .Replace("{timestamp}",   timestamp);

        var ext = CodecExtensions.TryGetValue(channel.ChannelType ?? "", out var e) ? e : ".ts";
        return name + ext;
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
    }
}
