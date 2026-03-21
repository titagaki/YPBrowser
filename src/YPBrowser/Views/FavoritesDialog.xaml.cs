using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;
using YPBrowser.Models;
using YPBrowser.Services;
using YPBrowser.ViewModels;

namespace YPBrowser.Views;

public sealed partial class FavoritesDialog : Window
{
    public FavoritesViewModel ViewModel { get; }
    private readonly SettingsService _settings;

    public FavoritesDialog(FavoritesViewModel viewModel, SettingsService settings)
    {
        ViewModel = viewModel;
        _settings = settings;
        InitializeComponent();
        this.SetWindowSize(680, 480);
        this.CenterOnScreen();
    }

    private void Target_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFavorite == null) return;

        var fields = FavoriteTargetFields.None;
        if (TargetName.IsChecked == true) fields |= FavoriteTargetFields.ChannelName;
        if (TargetGenre.IsChecked == true) fields |= FavoriteTargetFields.Genre;
        if (TargetDesc.IsChecked == true) fields |= FavoriteTargetFields.Description;
        if (TargetComment.IsChecked == true) fields |= FavoriteTargetFields.Comment;
        if (TargetContact.IsChecked == true) fields |= FavoriteTargetFields.ContactUrl;
        if (TargetYp.IsChecked == true) fields |= FavoriteTargetFields.YpName;
        if (TargetType.IsChecked == true) fields |= FavoriteTargetFields.ChannelType;
        if (TargetTrackTitle.IsChecked == true) fields |= FavoriteTargetFields.TrackTitle;
        if (TargetTrackArtist.IsChecked == true) fields |= FavoriteTargetFields.TrackArtist;

        // Store as list of field names in settings
        ViewModel.SelectedFavorite.TargetFields = fields.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s != "None")
            .ToList();
        if (ViewModel.SelectedFavorite.TargetFields.Count == 0)
            ViewModel.SelectedFavorite.TargetFields = ["ChannelName"];
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveAsync();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
