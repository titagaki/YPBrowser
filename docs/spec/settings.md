# 仕様: 設定

対象: `SettingsService`, `AppSettings`, 設定ダイアログ各ページ

## 1. 保存場所と形式

```
%AppData%\YPBrowser\settings.json
```

| 項目 | 仕様 |
|---|---|
| シリアライザ | `System.Text.Json` |
| 整形 | `WriteIndented = true` |
| プロパティ名 | C# のプロパティ名そのまま（命名ポリシー変換なし） |
| 列挙型 | 名前で保存（`JsonStringEnumConverter`）。例: `"DefaultAction": "Hidden"` |
| ファイルが無い場合 | 既定値を使う（このタイミングでは書き込まない） |
| 読み込みに失敗した場合 | エラーログを出して既定値を使う（例外は投げない） |
| 保存に失敗した場合 | エラーログを出すだけ（例外は投げない） |

`ISettingsService.Current` がアプリ全体で唯一の設定インスタンス。

## 2. 読み書きのタイミング

| 契機 | 動作 |
|---|---|
| メインウィンドウ `Loaded` | `LoadAsync()` → 通知サービス初期化 → 自動更新開始 → ウィンドウサイズ復元 |
| 旧形式からの移行 | `LoadAsync()` の中で変換できたら即 `SaveAsync()`（途中で落ちても変換をやり直さないため） |
| 設定ダイアログ「OK」 | YP サーバー・プレイヤー・自動ダウンロードルールの一覧を差し替えて `SaveAsync()` |
| ルール編集ダイアログ「OK」 | `Rules` と `Tags` を差し替えて `SaveAsync()` → 再判定 → 表示更新 |
| タグ設定ダイアログ「OK」 | `Tags` を差し替え、消えたタグの ID をルールから除いて `SaveAsync()` → 再判定 → 表示更新 |
| 一覧の星のトグル | 自動生成ルールを追加・削除して即 `SaveAsync()` → 再判定 → 表示更新 |
| メインウィンドウ `Closing` | `Window.Width` / `Height` / `SplitterPosition` を現在値で更新して `SaveAsync()` |

## 3. 全設定項目

「適用」列は、設定値が実際にアプリの動作へ反映されるかを示す。

### 3.1 `YpServers[]`（`YpServerSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Name` | string | `""` | ✔ | YP 名。ログの分離キー・`YpName` マッチ対象 |
| `Url` | string | `""` | ✔ | index.txt の URL |
| `Host` | string | `""` | ✔ | ローカル PeerCast の `ホスト:ポート`。取得 URL の `?host=` と `StreamUrl` に使う。空なら `localhost:7144` |
| `Enabled` | bool | `true` | ✔ | false の YP は取得しない |
| `BitrateMin` | int | `0` | ✔ | 下限。`0` でフィルタなし |
| `BitrateMax` | int | `-1` | ✔ | 上限。`0` 以下でフィルタなし |
| `TypeFilter` | string | `".*"` | ✔ | コーデックの正規表現。`".*"`・空でフィルタなし |

既定の YP サーバー（設定ファイルが無い場合）:

| Name | Url |
|---|---|
| SP | `http://bayonet.ddo.jp/sp/index.txt` |
| p@YP | `https://p-at.net/index.txt` |
| 0yp | `https://yayaue.me/yp/index.txt` |

### 3.2 `Tags[]`（`TagDefinition`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Id` | string | ランダム 32 桁 | ✔ | 不変。ルールはこれで参照する |
| `Name` | string | `""` | ✔ | 表示名 |
| `ForeColor` | string? | `null` | ✔ | 文字色（`#RRGGBB`）。null で指定なし |
| `BackColor` | string? | `null` | ✔ | 背景色（`#RRGGBB`）。null で指定なし |
| `DefaultAction` | enum | `Normal` | ✔ | `Normal` / `Highlight` / `Hidden` |
| `Notify` | bool | `false` | ✔ | 新着時にトーストを出す |
| `SoundPath` | string? | `null` | ✔ | 通知音の wav。null・空・不在なら既定音 |
| `BuiltIn` | bool | `false` | ✔ | 削除不可。読み込み時に ID から付け直される |

並び順が**行の色の優先順**（色を持つ最初のタグが勝つ）とビュー欄の並びを決める。

既定値（設定ファイルが無い場合）は組み込みタグ 2 件のみ。

| Id | Name | DefaultAction | Notify | 色 |
|---|---|---|---|---|
| `builtin-favorite` | お気に入り | `Highlight` | `true` | 背景 `#FFF4CE` / 文字 `#4A3A00` |
| `builtin-ng` | NG | `Hidden` | `false` | なし |

### 3.2.1 `Rules[]`（`Rule`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Id` | string | ランダム 32 桁 | ✔ | |
| `Name` | string | `""` | ✔ | 一覧に表示するルール名 |
| `Enabled` | bool | `true` | ✔ | false でルールを無視 |
| `Order` | int | `0` | ✔ | 小さいほど先に評価。OK 時に 0 から振り直す |
| `Combinator` | enum | `And` | ✔ | `And` / `Or` |
| `Conditions` | `RuleCondition[]` | `[]` | ✔ | 0 件のルールは常に不一致 |
| `TagIds` | string[] | `[]` | ✔ | 付与するタグ。実在しない ID は無視される |
| `StopProcessing` | bool | `false` | ✔ | 一致したら以降のルールを評価しない |
| `IsAuto` | bool | `false` | ✔ | 星ボタンが生成したルール |

`RuleCondition`:

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Field` | enum | `Description` | ✔ | `ChannelName` / `Description` / `ContactUrl` / `TrackArtist`（[matching.md](matching.md#条件-rulecondition)） |
| `MatchType` | enum | `Regex` | ✔ | `Contains` / `Exact` / `Regex` |
| `Negate` | bool | `false` | ✔ | 判定を反転 |
| `Pattern` | string | `""` | ✔ | 空なら常に不一致 |

### 3.2.2 `Favorites[]`（`FavoriteSettings`・旧形式）

読み込み時に `Tags` / `Rules` へ移行され、移行後は空になって保存ファイルから消える。
変換規則は [matching.md](matching.md#8-旧お気に入り形式からの移行-settingsmigration)。
`Rules` がすでに 1 件以上ある場合は取り込まない。

### 3.3 `Players[]`（`PlayerSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Name` | string | `""` | ✔ | 表示名・ログ出力名 |
| `ExecutablePath` | string | `""` | ✔ | 実行ファイルのパス |
| `ArgumentTemplate` | string | `"\"{url}\""` | ✔ | 引数。`{url}` が `StreamUrl` に置換される |
| `IsDefault` | bool | `false` | ✔ | 再生時に使うプレイヤーの選択に使う |

### 3.4 `AutoDownloadRules[]`（`AutoDownloadRuleSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Title` | string | `""` | ✔ | ルール名 |
| `Word` | string | `""` | ✔ | 検索語または正規表現 |
| `TargetFields` | string[] | `["ChannelName"]` | ✔ | マッチ対象 |
| `IsRegex` | bool | `false` | ✔ | true で正規表現 |
| `Enabled` | bool | `true` | ✔ | false でルールを無視 |

### 3.5 `Downloader`（`DownloaderSettings`・単一）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `OutputDirectory` | string | `""` | ✔ | 保存先。空なら `%USERPROFILE%\Downloads` |
| `FileNameTemplate` | string | `"{channelName}_{timestamp}"` | ✔ | ファイル名テンプレート |

### 3.6 `Network`（`NetworkSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `ProxyUrl` | string | `""` | ✘ | 未適用 |
| `UserAgent` | string | `"YPBrowser/1.0"` | ✘ | 未適用（同じ値がコードにハードコードされている） |
| `TimeoutSeconds` | int | `10` | 一部 | YP 取得のみ。下限 5 秒、上限は HttpClient 側の 10 秒 |

### 3.7 `Display`（`DisplaySettings`）

| キー | 型 | 既定値 | 適用 |
|---|---|---|---|
| `FontFamily` | string | `"Yu Gothic UI"` | ✘ |
| `FontSize` | double | `13` | ✘ |
| `BackgroundColor` | string | `"#FFFFFF"` | ✘ |
| `TextColor` | string | `"#1A1A1A"` | ✘ |
| `FavoriteNameColor` | string | `"#0000CC"` | ✘ |
| `NewChannelColor` | string | `"#006600"` | ✘ |
| `SelectedColor` | string | `"#0078D4"` | ✘ |

表示は XAML と `ChannelDiffToColorConverter` の固定値で描画される（[ui.md](ui.md#22-色)）。
`FontFamily` / `FontSize` / `FavoriteNameColor` / `NewChannelColor` は設定ダイアログで編集・保存できるが反映されない。
行の色はタグ側の設定で決まる（[matching.md](matching.md#5-表示への反映)）。

### 3.8 `Notifications`（`NotificationSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Enabled` | bool | `true` | ✔ | false でトースト通知を出さない |
| `SoundEnabled` | bool | `false` | ✘ | 未適用（UI にも無い） |
| `SoundFile` | string | `""` | ✘ | 未適用（UI にも無い） |
| `BalloonTimeoutSeconds` | int | `5` | ✘ | 未適用（UI では編集可） |

### 3.9 `Behavior`（`BehaviorSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `RefreshIntervalSeconds` | int | `60` | ✔ | 自動更新間隔。下限 30 秒 |
| `StartMinimized` | bool | `false` | ✘ | 未適用 |
| `MinimizeToTray` | bool | `true` | ✘ | 未適用（トレイアイコン自体が未実装） |
| `OpenOnDoubleClick` | bool | `true` | ✘ | 未適用（ダブルクリック再生は常に有効） |
| `NotifyOnFavorite` | bool | `true` | ✔ | false で通知タグの新着通知を出さない（タグごとの `Notify` より優先） |
| `ActiveFilterIndex` | int | `0` | ✘ | 未適用。ビュー選択は保存も復元もされず、起動時は常に「すべて」 |

### 3.10 `Window`（`WindowSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Width` | double | `900` | ✔ | 起動時に復元（`> 0` のとき）、終了時に保存 |
| `Height` | double | `600` | ✔ | 同上（`Width > 0` のときに一緒に復元） |
| `X` | double | `-1` | ✘ | 保存も復元もされない |
| `Y` | double | `-1` | ✘ | 保存も復元もされない |
| `SplitterPosition` | double | `150` | ✔ | 詳細パネルの高さ。起動時に復元（`> 0` のとき）、終了時に保存 |

## 4. 設定ダイアログのキャンセル挙動

設定ダイアログは「OK」で `SaveAsync()`、「キャンセル」は何もせず閉じるだけ。
編集内容がメモリ上の設定に残るかどうかは項目の種類で異なる。

| 編集内容 | キャンセル時 |
|---|---|
| YP サーバー・プレイヤー・自動ダウンロードルールの**追加 / 削除 / 並べ替え** | 破棄される（`ObservableCollection` のみを変更し、OK 時に `AppSettings` へ差し替えるため） |
| 上記の**各項目のフィールド編集**（名前・URL など） | 残る（一覧は同じインスタンスを参照しているため in-place で書き換わる） |
| 表示・ネットワーク・通知・動作・ダウンロード（保存先・ファイル名）ページ | 残る（`AppSettings` の各セクションを直接書き換えるため） |

キャンセルで残った変更は JSON へ即書き込まれないが、その後に保存契機（星のトグル、
アプリ終了時）があれば永続化される。

## 5. 設定 UI の入力反映パターン

各ページのコードビハインドは「リスト選択 → テキストボックスへ反映 → 入力 → 設定オブジェクトへ書き戻し」
という流れを取る。反映時の `TextChanged` で設定が上書きされるのを防ぐため `_loading` フラグを使う。

```csharp
private bool _loading;

private void List_SelectionChanged(...)
{
    _loading = true;
    NameBox.Text = ViewModel.SelectedItem?.Name ?? "";
    _loading = false;
}

private void NameBox_TextChanged(...)
{
    if (!_loading) ViewModel.SelectedItem!.Name = NameBox.Text;
}
```

数値入力（更新間隔・タイムアウト等）は `TryParse` に成功したときだけ設定へ反映され、
失敗した入力は無視される（エラー表示はない）。
