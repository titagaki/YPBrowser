using System.Windows;
using System.Windows.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public partial class NotificationsPage : UserControl
{
    private readonly SettingsService _settings;
    private bool _loading;

    public NotificationsPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        _loading = true;
        var n = _settings.Current.Notifications;
        EnabledCheck.IsChecked = n.Enabled;
        TimeoutBox.Text = n.BalloonTimeoutSeconds.ToString();
        _loading = false;
    }

    private void EnabledCheck_Click(object sender, RoutedEventArgs e)
    {
        if (!_loading) _settings.Current.Notifications.Enabled = EnabledCheck.IsChecked == true;
    }

    private void TimeoutBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && int.TryParse(TimeoutBox.Text, out var v))
            _settings.Current.Notifications.BalloonTimeoutSeconds = v;
    }
}
