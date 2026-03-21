using System.Windows.Controls;
using YPBrowser.Abstractions;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class NetworkPage : UserControl
{
    public NetworkPage(ISettingsService settings)
    {
        InitializeComponent();
        DataContext = new NetworkPageViewModel(settings.Current.Network);
    }
}
