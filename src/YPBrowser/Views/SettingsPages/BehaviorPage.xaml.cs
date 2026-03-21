using System.Windows.Controls;
using YPBrowser.Abstractions;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class BehaviorPage : UserControl
{
    public BehaviorPage(ISettingsService settings)
    {
        InitializeComponent();
        DataContext = new BehaviorPageViewModel(settings.Current.Behavior);
    }
}
