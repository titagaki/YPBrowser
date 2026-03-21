using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using YPBrowser.Abstractions;
using YPBrowser.Services;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
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
        services.AddHttpClient<YpFetchService>((sp, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
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
        services.AddSingleton<IFavoriteMatchService, FavoriteMatchService>();
        services.AddSingleton<IChannelDiffService, ChannelDiffService>();
        services.AddSingleton<IChannelFilterService, ChannelFilterService>();
        services.AddSingleton<IAutoRefreshService, AutoRefreshService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPlayerLaunchService, PlayerLaunchService>();
        services.AddSingleton<IRecordService, RecordService>();
        services.AddSingleton<IAutoDownloadMatchService, AutoDownloadMatchService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<FavoritesViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
