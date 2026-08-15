using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YPBrowser.Abstractions;
using YPBrowser.Models;

namespace YPBrowser.ViewModels;

/// <summary>ルール一覧の1行。付与するタグをチップで出すために、ID を解決したタグを持つ。</summary>
public partial class RuleRowViewModel : ObservableObject
{
    public Rule Rule { get; }

    public ObservableCollection<TagDefinition> Tags { get; } = [];

    public RuleRowViewModel(Rule rule) => Rule = rule;

    public void RefreshTags(IEnumerable<TagDefinition> allTags)
    {
        var byId = allTags.ToDictionary(t => t.Id);
        Tags.Clear();
        foreach (var id in Rule.TagIds)
        {
            if (byId.TryGetValue(id, out var tag)) Tags.Add(tag);
        }
    }
}

/// <summary>
/// ルール編集画面。編集は複製に対して行い、OK のときだけ設定へ書き戻す。
/// 条件を変えるたびに、現在読み込んでいる一覧に対してその場で件数を出し直す。
/// </summary>
public partial class RulesViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ITagMatchService _tagService;

    private IReadOnlyList<ChannelItem> _channels = [];
    private Rule? _watched;

    public ObservableCollection<RuleRowViewModel> Rules { get; } = [];

    /// <summary>付与できるタグ。未知の名前を入力すると、ここに新しいタグが増える。</summary>
    public ObservableCollection<TagDefinition> AvailableTags { get; } = [];

    /// <summary>「該当を確認」で出す実際の該当行。</summary>
    public ObservableCollection<ChannelItem> MatchPreview { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRule))]
    private RuleRowViewModel? _selectedRow;

    public Rule? SelectedRule => SelectedRow?.Rule;

    /// <summary>不正な正規表現のインラインエラー。空でなければ OK を通さない。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationError))]
    private string _validationError = "";

    public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MatchCountText))]
    private int _matchCount;

    public string MatchCountText => $"現在の一覧で {MatchCount} 件にマッチ";

    [ObservableProperty] private bool _isPreviewVisible;

    public RulesViewModel(ISettingsService settings, ITagMatchService tagService)
    {
        _settings = settings;
        _tagService = tagService;
        Load();
    }

    /// <summary>ライブ評価に使う、現在読み込んでいるチャンネル一覧を渡す。</summary>
    public void SetChannels(IReadOnlyList<ChannelItem> channels)
    {
        _channels = channels;
        Reevaluate();
    }

    /// <summary>右クリック「この条件でルールを作成」から、初期値入りのルールで開く。</summary>
    public void StartWith(Rule rule)
    {
        var row = new RuleRowViewModel(rule);
        row.RefreshTags(AvailableTags);
        Rules.Add(row);
        SelectedRow = row;
    }

    private void Load()
    {
        AvailableTags.Clear();
        foreach (var tag in _settings.Current.Tags)
            AvailableTags.Add(tag.Clone());

        Rules.Clear();
        foreach (var rule in _settings.Current.Rules.OrderBy(r => r.Order))
        {
            var row = new RuleRowViewModel(rule.Clone());
            row.RefreshTags(AvailableTags);
            Rules.Add(row);
        }

        SelectedRow = Rules.FirstOrDefault();
    }

    partial void OnSelectedRowChanged(RuleRowViewModel? oldValue, RuleRowViewModel? newValue)
    {
        Unwatch();
        Watch(newValue?.Rule);
        IsPreviewVisible = false;
        Reevaluate();
    }

    // --- ルール操作 ---

    [RelayCommand]
    private void AddRule()
    {
        var rule = new Rule
        {
            Name = "新しいルール",
            Order = Rules.Count,
            Conditions = [new RuleCondition()],
        };
        var row = new RuleRowViewModel(rule);
        Rules.Add(row);
        SelectedRow = row;
    }

    [RelayCommand]
    private void DuplicateRule()
    {
        if (SelectedRow is null) return;
        var copy = SelectedRow.Rule.Clone(newId: true);
        copy.Name = $"{copy.Name} のコピー";
        // 複製したものは手で編集する前提なので、自動生成（星）の印は外す
        copy.IsAuto = false;

        var row = new RuleRowViewModel(copy);
        row.RefreshTags(AvailableTags);
        Rules.Insert(Rules.IndexOf(SelectedRow) + 1, row);
        SelectedRow = row;
    }

    [RelayCommand]
    private void RemoveRule()
    {
        if (SelectedRow is null) return;
        var idx = Rules.IndexOf(SelectedRow);
        Rules.Remove(SelectedRow);
        SelectedRow = Rules.Count == 0 ? null : Rules[Math.Min(idx, Rules.Count - 1)];
    }

    [RelayCommand]
    private void MoveRuleUp()
    {
        if (SelectedRow is null) return;
        var idx = Rules.IndexOf(SelectedRow);
        if (idx > 0) Rules.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveRuleDown()
    {
        if (SelectedRow is null) return;
        var idx = Rules.IndexOf(SelectedRow);
        if (idx >= 0 && idx < Rules.Count - 1) Rules.Move(idx, idx + 1);
    }

    // --- 条件操作 ---

    [RelayCommand]
    private void AddCondition() => SelectedRule?.Conditions.Add(new RuleCondition());

    [RelayCommand]
    private void RemoveCondition(RuleCondition? condition)
    {
        if (condition is null) return;
        SelectedRule?.Conditions.Remove(condition);
    }

    [RelayCommand]
    private void ShowMatches()
    {
        MatchPreview.Clear();
        if (SelectedRule is not null && !HasValidationError)
        {
            foreach (var ch in _tagService.GetMatches(_channels, SelectedRule))
                MatchPreview.Add(ch);
        }
        IsPreviewVisible = true;
    }

    [RelayCommand]
    private void HideMatches() => IsPreviewVisible = false;

    // --- タグ操作 ---

    /// <summary>未知の名前を入力したらタグを新規作成する。</summary>
    [RelayCommand]
    private void AddTagByName(string? name)
    {
        if (SelectedRow is null || string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();

        var tag = AvailableTags.FirstOrDefault(t =>
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

        if (tag is null)
        {
            tag = new TagDefinition { Name = name };
            AvailableTags.Add(tag);
        }

        if (!SelectedRow.Rule.TagIds.Contains(tag.Id))
        {
            SelectedRow.Rule.TagIds.Add(tag.Id);
            SelectedRow.RefreshTags(AvailableTags);
        }
    }

    [RelayCommand]
    private void RemoveTag(TagDefinition? tag)
    {
        if (SelectedRow is null || tag is null) return;
        SelectedRow.Rule.TagIds.Remove(tag.Id);
        SelectedRow.RefreshTags(AvailableTags);
    }

    // --- ライブ評価 ---

    private void Watch(Rule? rule)
    {
        if (rule is null) return;
        _watched = rule;
        rule.PropertyChanged += OnRuleChanged;
        rule.Conditions.CollectionChanged += OnConditionsChanged;
        foreach (var c in rule.Conditions) c.PropertyChanged += OnRuleChanged;
    }

    private void Unwatch()
    {
        if (_watched is null) return;
        _watched.PropertyChanged -= OnRuleChanged;
        _watched.Conditions.CollectionChanged -= OnConditionsChanged;
        foreach (var c in _watched.Conditions) c.PropertyChanged -= OnRuleChanged;
        _watched = null;
    }

    private void OnRuleChanged(object? sender, PropertyChangedEventArgs e) => Reevaluate();

    private void OnConditionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var c in e.OldItems?.OfType<RuleCondition>() ?? [])
            c.PropertyChanged -= OnRuleChanged;
        foreach (var c in e.NewItems?.OfType<RuleCondition>() ?? [])
            c.PropertyChanged += OnRuleChanged;
        Reevaluate();
    }

    private void Reevaluate()
    {
        if (SelectedRule is null)
        {
            ValidationError = "";
            MatchCount = 0;
            MatchPreview.Clear();
            return;
        }

        // 不正な正規表現はその場で赤字にする（ダイアログは閉じさせない）
        ValidationError = SelectedRule.Conditions
            .Select(_tagService.ValidatePattern)
            .FirstOrDefault(e => e is not null) ?? "";

        MatchCount = HasValidationError
            ? 0
            : _tagService.GetMatches(_channels, SelectedRule).Count;

        if (IsPreviewVisible) ShowMatches();
    }

    /// <summary>OK を押せるか。どのルールにも不正な正規表現が無いこと。</summary>
    public string? FindBlockingError()
    {
        foreach (var row in Rules)
        {
            var error = row.Rule.Conditions
                .Select(_tagService.ValidatePattern)
                .FirstOrDefault(e => e is not null);
            if (error is not null) return $"「{row.Rule.Name}」の正規表現が不正です: {error}";
        }
        return null;
    }

    public async Task SaveAsync()
    {
        // 一覧の並び = 評価順
        for (int i = 0; i < Rules.Count; i++)
            Rules[i].Rule.Order = i;

        _settings.Current.Rules = [.. Rules.Select(r => r.Rule)];
        _settings.Current.Tags = [.. AvailableTags];
        await _settings.SaveAsync();
    }
}
