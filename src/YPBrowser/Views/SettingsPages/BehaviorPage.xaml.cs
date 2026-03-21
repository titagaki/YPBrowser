using System.Windows;
using System.Windows.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public partial class BehaviorPage : UserControl
{
    private readonly SettingsService _settings;
    private bool _loading;

    public BehaviorPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        _loading = true;
        var b = _settings.Current.Behavior;
        IntervalBox.Text = b.RefreshIntervalSeconds.ToString();
        MinimizeToTrayCheck.IsChecked = b.MinimizeToTray;
        StartMinimizedCheck.IsChecked = b.StartMinimized;
        NotifyFavCheck.IsChecked = b.NotifyOnFavorite;
        DoubleClickCheck.IsChecked = b.OpenOnDoubleClick;
        _loading = false;
    }

    private void IntervalBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && int.TryParse(IntervalBox.Text, out var v))
            _settings.Current.Behavior.RefreshIntervalSeconds = v;
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        if (!_loading) _settings.Current.Behavior.MinimizeToTray = MinimizeToTrayCheck.IsChecked == true;
    }

    private void StartMinimized_Click(object sender, RoutedEventArgs e)
    {
        if (!_loading) _settings.Current.Behavior.StartMinimized = StartMinimizedCheck.IsChecked == true;
    }

    private void NotifyFav_Click(object sender, RoutedEventArgs e)
    {
        if (!_loading) _settings.Current.Behavior.NotifyOnFavorite = NotifyFavCheck.IsChecked == true;
    }

    private void DoubleClick_Click(object sender, RoutedEventArgs e)
    {
        if (!_loading) _settings.Current.Behavior.OpenOnDoubleClick = DoubleClickCheck.IsChecked == true;
    }
}
