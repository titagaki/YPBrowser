using Microsoft.UI.Xaml.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public sealed partial class NotificationsPage : Page
{
    private readonly SettingsService _settings;

    public NotificationsPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        var n = _settings.Current.Notifications;
        EnabledSwitch.IsOn = n.Enabled;
        TimeoutBox.Value = n.BalloonTimeoutSeconds;
    }

    private void EnabledSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _settings.Current.Notifications.Enabled = EnabledSwitch.IsOn;

    private void TimeoutBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        => _settings.Current.Notifications.BalloonTimeoutSeconds = (int)args.NewValue;
}
