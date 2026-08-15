using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Models;

namespace YPBrowser.ViewModels;

/// <summary>
/// タグ設定画面。編集は複製に対して行い、OK のときだけ設定へ書き戻す。
/// </summary>
public partial class TagsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    public ObservableCollection<TagRowViewModel> Tags { get; } = [];

    [ObservableProperty] private TagRowViewModel? _selectedTag;

    public TagsViewModel(ISettingsService settings)
    {
        _settings = settings;
        Load();
    }

    private void Load()
    {
        Tags.Clear();
        foreach (var tag in _settings.Current.Tags)
            Tags.Add(new TagRowViewModel(tag.Clone(), UsageCount(tag.Id)));
    }

    /// <summary>そのタグを付けているルールの数。0 のタグは削除候補として分かる。</summary>
    private int UsageCount(string tagId) =>
        _settings.Current.Rules.Count(r => r.TagIds.Contains(tagId));

    [RelayCommand]
    private void AddTag()
    {
        var row = new TagRowViewModel(new TagDefinition { Name = "新しいタグ" }, 0);
        Tags.Add(row);
        SelectedTag = row;
    }

    [RelayCommand]
    private void RemoveTag()
    {
        // 組み込みタグ（お気に入り / NG）は削除できない
        if (SelectedTag is null || SelectedTag.Tag.BuiltIn) return;
        Tags.Remove(SelectedTag);
    }

    /// <summary>タグの並び順は、行の色をどれで塗るかの優先順になる。</summary>
    [RelayCommand]
    private void MoveTagUp()
    {
        if (SelectedTag is null) return;
        var idx = Tags.IndexOf(SelectedTag);
        if (idx > 0) Tags.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveTagDown()
    {
        if (SelectedTag is null) return;
        var idx = Tags.IndexOf(SelectedTag);
        if (idx >= 0 && idx < Tags.Count - 1) Tags.Move(idx, idx + 1);
    }

    public async Task SaveAsync()
    {
        _settings.Current.Tags = [.. Tags.Select(r => r.Tag)];

        // 消したタグを参照したままのルールが残らないようにする
        var live = _settings.Current.Tags.Select(t => t.Id).ToHashSet();
        foreach (var rule in _settings.Current.Rules)
        {
            var stale = rule.TagIds.Where(id => !live.Contains(id)).ToList();
            foreach (var id in stale) rule.TagIds.Remove(id);
        }

        await _settings.SaveAsync();
    }
}

/// <summary>タグ設定画面の1行。「使用」列のためだけに使用数を持つ。</summary>
public partial class TagRowViewModel : ObservableObject
{
    public TagDefinition Tag { get; }

    /// <summary>そのタグを付けているルールの数。</summary>
    public int UsageCount { get; }

    public bool CanDelete => !Tag.BuiltIn;

    public TagRowViewModel(TagDefinition tag, int usageCount)
    {
        Tag = tag;
        UsageCount = usageCount;
    }
}
