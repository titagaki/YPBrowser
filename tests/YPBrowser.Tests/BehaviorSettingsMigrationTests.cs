using System.Text.Json;
using YPBrowser.Helpers;
using YPBrowser.Settings;

namespace YPBrowser.Tests;

/// <summary>旧「動作」ページの設定から「全般」ページへの移行。</summary>
public class BehaviorSettingsMigrationTests
{
    [Fact]
    public void StartMinimizedTrue_BecomesMinimizedStartupState()
    {
        var settings = new AppSettings();
        settings.Behavior.StartMinimized = true;

        SettingsMigration.Migrate(settings);

        Assert.Equal(StartupWindowState.Minimized, settings.Behavior.StartupState);
        Assert.Null(settings.Behavior.StartMinimized);
    }

    [Fact]
    public void StartMinimizedFalse_KeepsNormalStartupState()
    {
        var settings = new AppSettings();
        settings.Behavior.StartMinimized = false;

        SettingsMigration.Migrate(settings);

        Assert.Equal(StartupWindowState.Normal, settings.Behavior.StartupState);
        Assert.Null(settings.Behavior.StartMinimized);
    }

    [Theory]
    [InlineData(true, MinimizeButtonAction.MinimizeToTray)]
    [InlineData(false, MinimizeButtonAction.KeepInTaskbar)]
    public void MinimizeToTray_BecomesMinimizeButtonAction(bool legacy, MinimizeButtonAction expected)
    {
        var settings = new AppSettings();
        settings.Behavior.MinimizeToTray = legacy;

        SettingsMigration.Migrate(settings);

        Assert.Equal(expected, settings.Behavior.MinimizeButtonAction);
        Assert.Null(settings.Behavior.MinimizeToTray);
    }

    [Fact]
    public void NotifyOnFavorite_MovesToNotifications()
    {
        var settings = new AppSettings();
        settings.Behavior.NotifyOnFavorite = false;

        SettingsMigration.Migrate(settings);

        Assert.False(settings.Notifications.NotifyOnFavorite);
        Assert.Null(settings.Behavior.NotifyOnFavorite);
    }

    [Fact]
    public void MissingLegacyValues_LeaveNewSettingsAlone()
    {
        var settings = new AppSettings();
        settings.Behavior.StartupState = StartupWindowState.Tray;
        settings.Behavior.MinimizeButtonAction = MinimizeButtonAction.MinimizeToTray;
        settings.Notifications.NotifyOnFavorite = false;

        SettingsMigration.Migrate(settings);

        Assert.Equal(StartupWindowState.Tray, settings.Behavior.StartupState);
        Assert.Equal(MinimizeButtonAction.MinimizeToTray, settings.Behavior.MinimizeButtonAction);
        Assert.False(settings.Notifications.NotifyOnFavorite);
    }

    [Theory]
    [InlineData(60, 60)]
    [InlineData(120, 120)]
    [InlineData(300, 300)]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    [InlineData(1, 60)]
    [InlineData(30, 60)]     // 選択肢から外した 30 秒は 60 秒へ
    [InlineData(45, 60)]
    [InlineData(90, 60)]     // 等距離は短い方
    [InlineData(100, 120)]
    [InlineData(210, 120)]   // 等距離は短い方
    [InlineData(999, 300)]
    public void RefreshInterval_IsRoundedToNearestPreset(int stored, int expected)
    {
        Assert.Equal(expected, SettingsMigration.RoundRefreshInterval(stored));

        var settings = new AppSettings();
        settings.Behavior.RefreshIntervalSeconds = stored;
        SettingsMigration.Migrate(settings);

        Assert.Equal(expected, settings.Behavior.RefreshIntervalSeconds);
    }

    [Fact]
    public void PresetValues_AreOfferedInTheDocumentedOrder()
    {
        Assert.Equal([60, 120, 300, 0], SettingsMigration.RefreshIntervalPresets);
    }

    /// <summary>
    /// 取得側で間引くのをやめたので、この下限がそのまま YP への実アクセス間隔の下限になる。
    /// </summary>
    [Fact]
    public void MinRefreshInterval_IsTheShortestPositivePreset()
    {
        Assert.Equal(60, SettingsMigration.MinRefreshIntervalSeconds);
    }

    [Fact]
    public void MigratedSettings_NoLongerWriteTheOldKeys()
    {
        var settings = new AppSettings();
        settings.Behavior.StartMinimized = true;
        settings.Behavior.MinimizeToTray = true;
        settings.Behavior.NotifyOnFavorite = true;

        SettingsMigration.Migrate(settings);
        var json = JsonSerializer.Serialize(settings.Behavior);

        // 値ではなくキーで見る（"MinimizeToTray" は新しい列挙の値名でもあるため）
        Assert.DoesNotContain("\"StartMinimized\"", json);
        Assert.DoesNotContain("\"MinimizeToTray\"", json);
        Assert.DoesNotContain("\"NotifyOnFavorite\"", json);
        Assert.Contains("\"StartupState\"", json);
        Assert.Contains("\"MinimizeButtonAction\"", json);
    }

    [Fact]
    public void MigrationIsIdempotent()
    {
        var settings = new AppSettings();
        settings.Behavior.StartMinimized = true;
        settings.Behavior.MinimizeToTray = true;
        settings.Behavior.NotifyOnFavorite = false;
        settings.Behavior.RefreshIntervalSeconds = 45;

        SettingsMigration.Migrate(settings);
        // 2 回目は書き換えるものが無いので、保存を促す必要も無い
        Assert.False(SettingsMigration.Migrate(settings));

        Assert.Equal(StartupWindowState.Minimized, settings.Behavior.StartupState);
        Assert.Equal(MinimizeButtonAction.MinimizeToTray, settings.Behavior.MinimizeButtonAction);
        Assert.False(settings.Notifications.NotifyOnFavorite);
        Assert.Equal(60, settings.Behavior.RefreshIntervalSeconds);
    }
}
