using Microsoft.UI.Xaml.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public sealed partial class BehaviorPage : Page
{
    private readonly SettingsService _settings;

    public BehaviorPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        var b = _settings.Current.Behavior;
        IntervalBox.Value = b.RefreshIntervalSeconds;
        MinimizeToTraySwitch.IsOn = b.MinimizeToTray;
        StartMinimizedSwitch.IsOn = b.StartMinimized;
        NotifyFavSwitch.IsOn = b.NotifyOnFavorite;
        DoubleClickSwitch.IsOn = b.OpenOnDoubleClick;
    }

    private void IntervalBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        => _settings.Current.Behavior.RefreshIntervalSeconds = (int)args.NewValue;

    private void MinimizeToTray_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _settings.Current.Behavior.MinimizeToTray = MinimizeToTraySwitch.IsOn;

    private void StartMinimized_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _settings.Current.Behavior.StartMinimized = StartMinimizedSwitch.IsOn;

    private void NotifyFav_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _settings.Current.Behavior.NotifyOnFavorite = NotifyFavSwitch.IsOn;

    private void DoubleClick_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _settings.Current.Behavior.OpenOnDoubleClick = DoubleClickSwitch.IsOn;
}
