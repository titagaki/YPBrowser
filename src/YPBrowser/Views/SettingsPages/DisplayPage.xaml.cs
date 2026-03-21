using System.Windows.Controls;
using YPBrowser.Abstractions;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class DisplayPage : UserControl
{
    public DisplayPage(ISettingsService settings)
    {
        InitializeComponent();
        DataContext = new DisplayPageViewModel(settings.Current.Display);
    }
}
