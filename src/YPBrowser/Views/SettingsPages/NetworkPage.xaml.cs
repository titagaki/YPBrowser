using System.Windows;
using System.Windows.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public partial class NetworkPage : UserControl
{
    private readonly SettingsService _settings;
    private bool _loading;

    public NetworkPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        _loading = true;
        var n = _settings.Current.Network;
        ProxyBox.Text = n.ProxyUrl;
        UserAgentBox.Text = n.UserAgent;
        TimeoutBox.Text = n.TimeoutSeconds.ToString();
        _loading = false;
    }

    private void ProxyBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) _settings.Current.Network.ProxyUrl = ProxyBox.Text;
    }

    private void UserAgentBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading) _settings.Current.Network.UserAgent = UserAgentBox.Text;
    }

    private void TimeoutBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading && int.TryParse(TimeoutBox.Text, out var v))
            _settings.Current.Network.TimeoutSeconds = v;
    }
}
