using System.Windows;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.ViewModels;

namespace YPBrowser.Views;

public partial class FavoritesDialog : Window
{
    public FavoritesViewModel ViewModel { get; }
    private readonly ISettingsService _settings;

    public FavoritesDialog(FavoritesViewModel viewModel, ISettingsService settings)
    {
        ViewModel = viewModel;
        _settings = settings;
        DataContext = viewModel;
        InitializeComponent();
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
