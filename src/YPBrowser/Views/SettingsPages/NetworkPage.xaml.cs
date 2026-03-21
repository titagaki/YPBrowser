using Microsoft.UI.Xaml.Controls;
using YPBrowser.Services;

namespace YPBrowser.Views.SettingsPages;

public sealed partial class NetworkPage : Page
{
    private readonly SettingsService _settings;

    public NetworkPage(SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        var n = _settings.Current.Network;
        ProxyBox.Text = n.ProxyUrl;
        UserAgentBox.Text = n.UserAgent;
        TimeoutBox.Value = n.TimeoutSeconds;
    }

    private void ProxyBox_TextChanged(object sender, TextChangedEventArgs e)
        => _settings.Current.Network.ProxyUrl = ProxyBox.Text;

    private void UserAgentBox_TextChanged(object sender, TextChangedEventArgs e)
        => _settings.Current.Network.UserAgent = UserAgentBox.Text;

    private void TimeoutBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        => _settings.Current.Network.TimeoutSeconds = (int)args.NewValue;
}
