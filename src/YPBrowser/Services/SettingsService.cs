using System.Text.Json;
using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;
using YPBrowser.Settings;

namespace YPBrowser.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YPBrowser");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };

    private readonly ILogger<SettingsService> _logger;

    public AppSettings Current { get; private set; } = CreateDefaults();

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                Current = CreateDefaults();
                return;
            }
            await using var fs = File.OpenRead(SettingsPath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOptions);
            Current = loaded ?? CreateDefaults();
            _logger.LogInformation("Settings loaded from {Path}", SettingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            Current = CreateDefaults();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            await using var fs = File.Create(SettingsPath);
            await JsonSerializer.SerializeAsync(fs, Current, JsonOptions);
            _logger.LogInformation("Settings saved to {Path}", SettingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    private static AppSettings CreateDefaults() => new()
    {
        YpServers =
        [
            new YpServerSettings
            {
                Name = "SP",
                Url = "http://bayonet.ddo.jp/sp/index.txt",
                Host = "",
                Enabled = true
            },
            new YpServerSettings
            {
                Name = "p@YP",
                Url = "https://p-at.net/index.txt",
                Host = "",
                Enabled = true
            },
            new YpServerSettings
            {
                Name = "0yp",
                Url = "https://yayaue.me/yp/index.txt",
                Host = "",
                Enabled = true
            }
        ],
        Players = [],
        Favorites = [],
    };
}
