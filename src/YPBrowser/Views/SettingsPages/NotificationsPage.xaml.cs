using System.Windows.Controls;
using YPBrowser.ViewModels;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class NotificationsPage : UserControl
{
    public NotificationsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = new NotificationsPageViewModel(viewModel.Draft.Notifications);
    }
}
