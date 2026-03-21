using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;
using YPBrowser.Helpers;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Services;

public class YpFetchService : IYpFetchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YpFetchService> _logger;

    private static readonly Regex TypeFilterCache = new(".*", RegexOptions.Compiled);

    public YpFetchService(HttpClient httpClient, ILogger<YpFetchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ChannelItem>> FetchAsync(YpServerItem server, NetworkSettings network, CancellationToken ct = default)
    {
        var url = BuildIndexUrl(server);
        _logger.LogDebug("Fetching YP {Name} from {Url}", server.Name, url);

        try
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, network.TimeoutSeconds));
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            // Strip BOM if present
            var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');

            var channels = ParseLines(text, server);
            server.LastUpdateTime = DateTime.Now;
            server.LastError = null;
            server.ChannelCount = channels.Count;
            _logger.LogInformation("Fetched {Count} channels from {Name}", channels.Count, server.Name);
            return channels;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Fetch cancelled for {Name}", server.Name);
            return [];
        }
        catch (Exception ex)
        {
            server.LastError = ex.Message;
            _logger.LogWarning(ex, "Failed to fetch YP {Name}", server.Name);
            return [];
        }
    }

    private static string BuildIndexUrl(YpServerItem server)
    {
        var baseUrl = server.Url.TrimEnd('/') + "/";
        var query = string.IsNullOrEmpty(server.Host) ? "" : $"?host={Uri.EscapeDataString(server.Host)}";
        return $"{baseUrl}index.txt{query}";
    }

    private List<ChannelItem> ParseLines(string text, YpServerItem server)
    {
        var result = new List<ChannelItem>();
        Regex? typeFilterRegex = null;
        if (!string.IsNullOrWhiteSpace(server.TypeFilter) && server.TypeFilter != ".*")
        {
            try { typeFilterRegex = new Regex(server.TypeFilter, RegexOptions.IgnoreCase); }
            catch { /* ignore invalid regex */ }
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim('\r', '\n', ' ');
            if (string.IsNullOrEmpty(line)) continue;

            var fields = line.Split(new[] { "<>" }, StringSplitOptions.None);
            if (fields.Length < 19) continue;

            var ch = new ChannelItem
            {
                ChannelName = HtmlSpecialCharsHelper.Decode(GetField(fields, 0)),
                Id = GetField(fields, 1),
                Host = GetField(fields, 2),
                ContactUrl = HtmlSpecialCharsHelper.Decode(GetField(fields, 3)),
                Genre = HtmlSpecialCharsHelper.Decode(GetField(fields, 4)),
                Description = HtmlSpecialCharsHelper.Decode(GetField(fields, 5)),
                Listeners = ParseInt(GetField(fields, 6)),
                Relays = ParseInt(GetField(fields, 7)),
                BitrateKbps = ParseInt(GetField(fields, 8)),
                ChannelType = GetField(fields, 9),
                TrackArtist = HtmlSpecialCharsHelper.Decode(GetField(fields, 10)),
                TrackAlbum = HtmlSpecialCharsHelper.Decode(GetField(fields, 11)),
                TrackTitle = HtmlSpecialCharsHelper.Decode(GetField(fields, 12)),
                TrackGenre = HtmlSpecialCharsHelper.Decode(GetField(fields, 13)),
                UrlParam = GetField(fields, 14),
                BroadcastTimeStr = GetField(fields, 15),
                KyasukoStatus = GetField(fields, 16),
                Comment = HtmlSpecialCharsHelper.Decode(GetField(fields, 17)),
                IsDirect = GetField(fields, 18) == "1",
                YpName = server.Name,
                YpUrl = server.Url.TrimEnd('/') + "/",
                FetchedAt = DateTime.Now,
                YpPriority = 0,
            };

            if (string.IsNullOrEmpty(ch.Id)) continue;

            // Bitrate filter
            if (server.BitrateMin > 0 && ch.BitrateKbps < server.BitrateMin) continue;
            if (server.BitrateMax > 0 && ch.BitrateKbps > server.BitrateMax) continue;

            // Type filter
            if (typeFilterRegex != null && !typeFilterRegex.IsMatch(ch.ChannelType)) continue;

            result.Add(ch);
        }

        return result;
    }

    private static string GetField(string[] fields, int index) =>
        index < fields.Length ? fields[index].Trim() : "";

    private static int ParseInt(string s)
    {
        if (int.TryParse(s, out var v)) return v;
        return -1;
    }
}
