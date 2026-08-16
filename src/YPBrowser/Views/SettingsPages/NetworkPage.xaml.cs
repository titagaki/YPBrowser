using System.Windows.Controls;
using YPBrowser.ViewModels;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class NetworkPage : UserControl
{
    public NetworkPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = new NetworkPageViewModel(viewModel.Draft.Network);
    }
}
