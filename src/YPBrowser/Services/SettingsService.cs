using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;
using YPBrowser.Helpers;
using YPBrowser.Models;
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
        // タグの既定の扱い・一致方式などを、数値ではなく読める名前で保存する
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ILogger<SettingsService> _logger;

    // 保存の契機は複数ある（設定の OK・星のトグル・終了時）ので、書き込み同士が重ならないようにする
    private readonly SemaphoreSlim _saveLock = new(1, 1);

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

            AppSettings? loaded;
            await using (var fs = File.OpenRead(SettingsPath))
            {
                loaded = await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOptions);
            }
            Current = loaded ?? CreateDefaults();
            _logger.LogInformation("Settings loaded from {Path}", SettingsPath);

            // 旧「お気に入り」形式からタグ方式への移行。移行できたらすぐ書き戻して、
            // 途中でクラッシュしても次回また変換し直さないようにする。
            if (SettingsMigration.Migrate(Current))
            {
                _logger.LogInformation(
                    "Migrated settings to tag model: {Tags} tags, {Rules} rules",
                    Current.Tags.Count, Current.Rules.Count);
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            Current = CreateDefaults();
        }
    }

    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
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
        finally
        {
            _saveLock.Release();
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
        Tags = [TagDefinition.CreateFavorite(), TagDefinition.CreateNg()],
        Rules = [],
    };
}
