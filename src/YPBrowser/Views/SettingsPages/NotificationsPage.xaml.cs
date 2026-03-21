using System.Windows.Controls;
using YPBrowser.Abstractions;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class NotificationsPage : UserControl
{
    public NotificationsPage(ISettingsService settings)
    {
        InitializeComponent();
        DataContext = new NotificationsPageViewModel(settings.Current.Notifications);
    }
}
