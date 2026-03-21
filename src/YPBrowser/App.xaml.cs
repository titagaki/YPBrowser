using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using YPBrowser.Services;
using YPBrowser.ViewModels;
using YPBrowser.Views;

namespace YPBrowser;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();
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

        // Services
        services.AddSingleton<SettingsService>();
        services.AddSingleton<YpFetchService>();
        services.AddSingleton<FavoriteMatchService>();
        services.AddSingleton<ChannelDiffService>();
        services.AddSingleton<AutoRefreshService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<PlayerLaunchService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<FavoritesViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
