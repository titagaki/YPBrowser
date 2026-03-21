using System.Diagnostics;
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

    private readonly ILogger<RecordService> _logger;

    public RecordService(ILogger<RecordService> logger)
    {
        _logger = logger;
    }

    public void Record(ChannelItem channel, DownloaderSettings settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.ExecutablePath))
            {
                _logger.LogWarning("No recorder executable configured");
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputDir = ResolveOutputDirectory(settings.OutputDirectory);
            var filename = BuildFilename(channel, settings.FileNameTemplate, timestamp);

            var args = settings.ArgumentTemplate
                .Replace("{url}",         channel.StreamUrl)
                .Replace("{outputDir}",   outputDir)
                .Replace("{channelName}", SanitizeFilename(channel.ChannelName))
                .Replace("{timestamp}",   timestamp)
                .Replace("{filename}",    filename);

            _logger.LogInformation("Starting recording: {Exe} {Args}", settings.ExecutablePath, args);

            Process.Start(new ProcessStartInfo
            {
                FileName        = settings.ExecutablePath,
                Arguments       = args,
                UseShellExecute = false,
                CreateNoWindow  = true,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recorder for {Channel}", channel.ChannelName);
            throw;
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
