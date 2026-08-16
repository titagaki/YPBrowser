using System.Windows.Controls;
using YPBrowser.ViewModels;
using YPBrowser.ViewModels.SettingsPages;

namespace YPBrowser.Views.SettingsPages;

public partial class DisplayPage : UserControl
{
    public DisplayPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = new DisplayPageViewModel(viewModel.Draft.Display);
    }
}
