# 設定システム

## 保存先とフォーマット

```
%AppData%\YPBrowser\settings.json
```

`System.Text.Json` でシリアライズ。
起動時に `SettingsService.LoadAsync()` で読み込み、
変更時に `SettingsService.SaveAsync()` で書き込む。

## AppSettings の階層構造

```
AppSettings
├─ YpServers[]          YP サーバー設定
├─ Favorites[]          お気に入りルール
├─ Players[]            外部プレイヤー設定
├─ AutoDownloadRules[]  自動ダウンロードルール
├─ Downloader           録音設定（1つのみ）
├─ Network              プロキシ・タイムアウト
├─ Display              フォント・色
├─ Notifications        通知設定
├─ Behavior             更新間隔・起動時動作
└─ Window               ウィンドウサイズ・位置
```

## なぜ Players はリストで Downloader は単一か

外部プレイヤーは用途別に複数登録したい（例: 音声は VLC、動画は MPC）。
録音ツールは「どこに保存するか」だけが設定でき、ツール自体は HttpClient 固定のため
設定は1つで十分。

## 設定 UI のパターン（`_loading` フラグ）

設定ページ（`YpServersPage`, `PlayersPage`, `AutoDownloadPage` 等）では
「リスト選択 → テキストボックスに反映 → テキストボックス変更 → 設定オブジェクトに書き戻す」
というパターンを使う。

テキストボックスへの反映時に `TextChanged` が発火して設定が書き変わるのを防ぐため、
`_loading` フラグを使う。

```csharp
private bool _loading;

private void List_SelectionChanged(...)
{
    _loading = true;       // TextChanged を無視する
    NameBox.Text = ViewModel.SelectedItem?.Name ?? "";
    _loading = false;
}

private void NameBox_TextChanged(...)
{
    if (!_loading)         // 手動入力のみ反映
        ViewModel.SelectedItem!.Name = NameBox.Text;
}
```

## 設定の 2 段階コミット（キャンセル対応）

`SettingsDialog` / `FavoritesDialog` は Transient ViewModel を使う。

```
ダイアログを開く
    │
    ↓ ViewModel 新規生成（現在の設定をコピーして ObservableCollection に格納）
    │
    ├─ [OK] → SaveAsync() → AppSettings を上書き → JSON 保存
    └─ [キャンセル] → ViewModel が GC で破棄 → 設定は変わらない
```

ダイアログ内で設定オブジェクト（`PlayerSettings` 等の POCO）を直接変更しているが、
`AppSettings.Players` リストとは**別の ObservableCollection**にコピーされているため、
キャンセル時に元のリストは変わらない。

### 注意: `DownloaderSettings` は in-place 変更

`AutoDownloadPage` の TextChanged ハンドラは
`ViewModel.Downloader.OutputDirectory = ...` と直接書き込む。
`Downloader` は `_settings.Current.Downloader` への参照なので、
キャンセルしても変更が残る。

これは `PlayerSettings` と同じ挙動（`PlayersPage` も同様）。
リスト型（YpServers, Players）はキャンセルで元に戻るが、
単一オブジェクト型（Downloader, Network 等）はキャンセルで戻らない現状の制約。

## BehaviorSettings の ActiveFilterIndex

```csharp
public int ActiveFilterIndex { get; set; } = 0;
```

フィルタ選択（すべて / 新着 / お気に入り / NG / ログ）の初期値を保存する。
アプリ終了時のフィルタ状態が次回起動時に復元される。

`BehaviorSettings` に含まれているのは「アプリの動作に関わる設定」という
分類上の判断。表示設定（`DisplaySettings`）ではなく動作設定に分類されている。

## YpServerSettings の TypeFilter

```csharp
public string TypeFilter { get; set; } = ".*";
```

正規表現でコーデックをフィルタする。デフォルト `".*"` は全許可。
`YpFetchService` がパース時に `Regex.IsMatch(ch.ChannelType, TypeFilter)` で評価する。

無効な正規表現を設定した場合：
- `YpFetchService` が例外を catch して**黙殺**
- フィルタが無効化され、全コーデックが通過する
- エラーメッセージや通知なし

設定 UI にはバリデーションがないため注意。
