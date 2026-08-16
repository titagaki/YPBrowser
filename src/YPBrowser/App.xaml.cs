using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using YPBrowser.Abstractions;
using YPBrowser.Services;
using YPBrowser.Settings;
using YPBrowser.ViewModels;
using YPBrowser.Views;

namespace YPBrowser;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static new MainWindow? MainWindow { get; private set; }

    public App()
    {
        Services = ConfigureServices();
    }

    /// <summary>
    /// 設定はここで読む。「起動時の状態」を見てからでないとウィンドウの出し方を決められないため
    /// （以前はウィンドウの Loaded で読んでいた）。
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();

        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.InitializeApp();

        switch (settings.Current.Behavior.StartupState)
        {
            case StartupWindowState.Tray:
                window.HideToTray();
                break;

            case StartupWindowState.Minimized:
                window.WindowState = WindowState.Minimized;
                window.Show();
                break;

            default:
                window.Show();
                break;
        }

        // Show() の中で走る最小化までは「起動時の状態」の指示として扱う
        window.MarkStartupComplete();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(b =>
        {
            b.AddDebug();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        // HttpClient
        // UserAgent はアプリが名乗る値なので、設定項目にはしない（相手側の識別に使われる）
        services.AddHttpClient<YpFetchService>((sp, client) =>
        {
            // 制限時間は取得ごとに Network.TimeoutSeconds で掛ける。ここで固定値を持つと
            // 設定より短い側が勝ってしまい、エラーに出す秒数と実際が食い違う
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("YPBrowser/1.0");
        });
        services.AddHttpClient("RecordService", client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("YPBrowser/1.0");
        });

        // Services (registered against their interfaces)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IYpFetchService>(sp => sp.GetRequiredService<YpFetchService>());
        services.AddSingleton<IYpServerStateService, YpServerStateService>();
        services.AddSingleton<ITagMatchService, TagMatchService>();
        services.AddSingleton<IChannelDiffService, ChannelDiffService>();
        services.AddSingleton<IChannelFilterService, ChannelFilterService>();
        services.AddSingleton<IAutoRefreshService, AutoRefreshService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<IPlayerLaunchService, PlayerLaunchService>();
        services.AddSingleton<IRecordService, RecordService>();
        services.AddSingleton<IAutoDownloadMatchService, AutoDownloadMatchService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TagsViewModel>();
        services.AddTransient<RulesViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
