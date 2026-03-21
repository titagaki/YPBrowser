using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Services;
using YPBrowser.Settings;

namespace YPBrowser.ViewModels;

public partial class FavoritesViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    public ObservableCollection<FavoriteSettings> Favorites { get; } = [];

    [ObservableProperty] private FavoriteSettings? _selectedFavorite;

    public FavoritesViewModel(SettingsService settings)
    {
        _settings = settings;
        Load();
    }

    private void Load()
    {
        Favorites.Clear();
        foreach (var f in _settings.Current.Favorites) Favorites.Add(f);
    }

    [RelayCommand]
    private void AddFavorite()
    {
        var f = new FavoriteSettings
        {
            Title = "新しいお気に入り",
            Word = "",
            TargetFields = ["ChannelName"],
            BackColor = "#FFFF99",
            TextColor = "#000000",
        };
        Favorites.Add(f);
        SelectedFavorite = f;
    }

    [RelayCommand]
    private void RemoveFavorite()
    {
        if (SelectedFavorite != null)
            Favorites.Remove(SelectedFavorite);
    }

    [RelayCommand]
    private void MoveFavoriteUp()
    {
        if (SelectedFavorite == null) return;
        var idx = Favorites.IndexOf(SelectedFavorite);
        if (idx > 0) Favorites.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveFavoriteDown()
    {
        if (SelectedFavorite == null) return;
        var idx = Favorites.IndexOf(SelectedFavorite);
        if (idx < Favorites.Count - 1) Favorites.Move(idx, idx + 1);
    }

    public async Task SaveAsync()
    {
        _settings.Current.Favorites = [.. Favorites];
        await _settings.SaveAsync();
    }
}
