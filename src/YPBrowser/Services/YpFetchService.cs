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

        var timeoutSec = Math.Max(5, network.TimeoutSeconds);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

        try
        {
            var response = await _httpClient.GetAsync(url, timeoutCts.Token);
            response.EnsureSuccessStatusCode();

            // \u672C\u6587\u306E\u8AAD\u307F\u51FA\u3057\u306B\u3082\u540C\u3058\u5236\u9650\u6642\u9593\u3092\u304B\u3051\u308B\u3002\u5FDC\u7B54\u30D8\u30C3\u30C0\u3060\u3051\u8FD4\u3057\u3066
            // \u672C\u6587\u304C\u6765\u306A\u3044\u76F8\u624B\u306B\u3001\u3044\u3064\u307E\u3067\u3082\u5F85\u305F\u3055\u308C\u306A\u3044\u3088\u3046\u306B\u3059\u308B\u305F\u3081
            var bytes = await response.Content.ReadAsByteArrayAsync(timeoutCts.Token);
            // Strip BOM if present
            var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');

            var channels = ParseLines(text, server);
            server.LastUpdateTime = DateTime.Now;
            server.LastError = null;
            server.ChannelCount = channels.Count;
            _logger.LogInformation("Fetched {Count} channels from {Name}", channels.Count, server.Name);
            return channels;
        }
        // \u30A2\u30D7\u30EA\u7D42\u4E86\u306A\u3069\u3067\u547C\u3073\u51FA\u3057\u5074\u304C\u6B62\u3081\u305F\u5834\u5408\u3002\u5931\u6557\u3067\u306F\u306A\u3044\u306E\u3067\u72B6\u614B\u306F\u5909\u3048\u306A\u3044
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Fetch cancelled for {Name}", server.Name);
            return [];
        }
        // \u5236\u9650\u6642\u9593\u5207\u308C\u3002OperationCanceledException \u3067\u98DB\u3093\u3067\u304F\u308B\u305F\u3081\u3001
        // \u4E0A\u306E\u30AD\u30E3\u30F3\u30BB\u30EB\u3068\u533A\u5225\u3057\u306A\u3044\u3068\u30A8\u30E9\u30FC\u3068\u3057\u3066\u8A18\u9332\u3055\u308C\u306A\u3044\u307E\u307E\u6D88\u3048\u308B
        catch (OperationCanceledException)
        {
            server.LastError = $"\u5FDC\u7B54\u304C\u3042\u308A\u307E\u305B\u3093\uFF08{timeoutSec} \u79D2\u3067\u30BF\u30A4\u30E0\u30A2\u30A6\u30C8\uFF09";
            _logger.LogWarning("Fetch timed out for {Name} after {Seconds}s", server.Name, timeoutSec);
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
        var query = string.IsNullOrEmpty(server.Host) ? "" : $"?host={Uri.EscapeDataString(server.Host)}";
        return $"{server.Url}{query}";
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
                YpHost = server.Host,
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
