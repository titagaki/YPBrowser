using System.Text.Json;
using YPBrowser.Helpers;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Tests;

/// <summary>タイプごとのプレイヤー: 選択・置換・旧形式からの移行。</summary>
public class PlayerSettingsTests
{
    private static PlayerSettings Player(string contentType, string args = "\"{stream}\"") =>
        new() { ContentType = contentType, ExecutablePath = $@"C:\p\{contentType}.exe", ArgumentTemplate = args };

    private static ChannelItem Channel() => new()
    {
        ChannelName = "いまいch",
        Id = "A1B2C3",
        Host = "192.0.2.10:7144",
        YpHost = "localhost:7144",
        ContactUrl = "http://example.test/",
        Genre = "音楽",
        Description = "詳細です",
        Comment = "コメントです",
        ChannelType = "FLV",
        IsDirect = true,
    };

    // --- 再生に使うプレイヤーの選択 ---

    [Fact]
    public void MatchingContentType_IsPreferredOverFallback()
    {
        List<PlayerSettings> players = [Player(PlayerContentTypes.Fallback), Player("FLV")];

        Assert.Equal("FLV", PlayerSelection.For(players, "FLV")?.ContentType);
    }

    [Fact]
    public void UnknownContentType_FallsBackToTheOtherEntry()
    {
        List<PlayerSettings> players = [Player("FLV"), Player(PlayerContentTypes.Fallback)];

        Assert.Equal(PlayerContentTypes.Fallback, PlayerSelection.For(players, "OGV")?.ContentType);
    }

    [Fact]
    public void ContentTypeMatch_IgnoresCase()
    {
        List<PlayerSettings> players = [Player("FLV")];

        Assert.NotNull(PlayerSelection.For(players, "flv"));
    }

    [Fact]
    public void NoFallbackAndNoMatch_SelectsNothing()
    {
        List<PlayerSettings> players = [Player("FLV")];

        // 呼び出し側は OS の既定ハンドラへ落とす
        Assert.Null(PlayerSelection.For(players, "MP3"));
        Assert.Null(PlayerSelection.For([], "FLV"));
    }

    // --- 引数の置換 ---

    [Fact]
    public void EveryPlaceholder_IsReplaced()
    {
        var channel = Channel();
        var template = string.Join(" ", PlayerPlaceholders.All.Select(p => p.Token));

        var expanded = PlayerPlaceholders.Expand(template, channel);

        Assert.DoesNotContain("{", expanded);
        Assert.Contains(channel.StreamUrl, expanded);
        Assert.Contains("いまいch", expanded);
        Assert.Contains("http://example.test/", expanded);
        Assert.Contains("音楽", expanded);
        Assert.Contains("詳細です", expanded);
        Assert.Contains("コメントです", expanded);
        Assert.Contains("FLV", expanded);
    }

    [Theory]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    public void Direct_IsWrittenAsTheYpFlag(bool isDirect, string expected)
    {
        var channel = Channel();
        channel.IsDirect = isDirect;

        Assert.Equal(expected, PlayerPlaceholders.Expand("{direct}", channel));
    }

    [Fact]
    public void UnknownToken_IsLeftAlone()
    {
        // プレイヤー自身の記法を壊さない
        Assert.Equal("{nosuch}", PlayerPlaceholders.Expand("{nosuch}", Channel()));
        Assert.Equal("\"<stream/>\"", PlayerPlaceholders.Expand("\"<stream/>\"", Channel()));
    }

    [Fact]
    public void LegacyUrlToken_StillExpands()
    {
        var channel = Channel();

        Assert.Equal(channel.StreamUrl, PlayerPlaceholders.Expand("{url}", channel));
    }

    [Fact]
    public void PresetArguments_UseOnlyKnownPlaceholders()
    {
        var channel = Channel();

        foreach (var preset in PlayerPresets.All)
        {
            var expanded = PlayerPlaceholders.Expand(preset.ArgumentTemplate, channel);
            Assert.DoesNotContain("{", expanded);
        }
    }

    // --- 旧形式からの移行 ---

    [Fact]
    public void LegacyDefaultPlayer_BecomesTheFallbackEntry()
    {
        var settings = new AppSettings
        {
            Players =
            [
                new() { Name = "VLC", ExecutablePath = @"C:\vlc.exe", ArgumentTemplate = "\"{url}\"" },
                new() { Name = "MPC", ExecutablePath = @"C:\mpc.exe", ArgumentTemplate = "\"{url}\" /play", IsDefault = true },
            ],
        };

        SettingsMigration.Migrate(settings);

        var player = Assert.Single(settings.Players);
        Assert.Equal(PlayerContentTypes.Fallback, player.ContentType);
        Assert.Equal(@"C:\mpc.exe", player.ExecutablePath);
        Assert.Equal("\"{stream}\" /play", player.ArgumentTemplate);
        Assert.Null(player.Name);
        Assert.Null(player.IsDefault);
    }

    [Fact]
    public void LegacyPlayersWithoutADefault_KeepTheFirstOne()
    {
        var settings = new AppSettings
        {
            Players =
            [
                new() { Name = "VLC", ExecutablePath = @"C:\vlc.exe" },
                new() { Name = "MPC", ExecutablePath = @"C:\mpc.exe" },
            ],
        };

        SettingsMigration.Migrate(settings);

        Assert.Equal(@"C:\vlc.exe", Assert.Single(settings.Players).ExecutablePath);
    }

    [Fact]
    public void MigratedPlayers_NoLongerWriteTheOldKeys()
    {
        var settings = new AppSettings
        {
            Players = [new() { Name = "VLC", ExecutablePath = @"C:\vlc.exe", IsDefault = true }],
        };

        SettingsMigration.Migrate(settings);
        var json = JsonSerializer.Serialize(settings.Players);

        Assert.DoesNotContain("\"Name\"", json);
        Assert.DoesNotContain("\"IsDefault\"", json);
        Assert.Contains("\"ContentType\"", json);
    }

    [Fact]
    public void PlayerMigrationIsIdempotent()
    {
        var settings = new AppSettings
        {
            Players = [new() { Name = "VLC", ExecutablePath = @"C:\vlc.exe", IsDefault = true }],
        };

        SettingsMigration.Migrate(settings);
        Assert.False(SettingsMigration.Migrate(settings));

        Assert.Equal(PlayerContentTypes.Fallback, Assert.Single(settings.Players).ContentType);
    }

    [Fact]
    public void NewFormatPlayers_AreLeftAlone()
    {
        var settings = new AppSettings { Players = [Player("FLV"), Player(PlayerContentTypes.Fallback)] };

        SettingsMigration.Migrate(settings);

        Assert.Equal(2, settings.Players.Count);
        Assert.Equal("FLV", settings.Players[0].ContentType);
    }

    // --- 並び順 ---

    [Fact]
    public void FallbackSortsLast()
    {
        var keys = new[] { "FLV", "RAW", PlayerContentTypes.Fallback }
            .Select(PlayerContentTypes.SortKey)
            .ToList();

        Assert.True(keys[0] < keys[1]);
        Assert.True(keys[1] < keys[2]);
    }

    [Fact]
    public void SelectableTypes_EndWithTheFallback()
    {
        Assert.Equal(PlayerContentTypes.Fallback, PlayerContentTypes.Selectable[^1]);
        Assert.Equal(PlayerContentTypes.Known.Length + 1, PlayerContentTypes.Selectable.Length);
    }
}
