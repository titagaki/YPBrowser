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
| 設定ダイアログ「OK」 | 編集していた複製を `AppSettings` へ書き戻して `SaveAsync()`（[4 章](#4-設定の反映と保存のタイミング)） |
| 設定ダイアログ「キャンセル」 | 何もしない（複製ごと捨てる） |
| ルール編集ダイアログ「OK」 | `Rules` と `Tags` を差し替えて `SaveAsync()` → 再判定 → 表示更新 |
| タグ設定ダイアログ「OK」 | `Tags` を差し替え、消えたタグの ID をルールから除いて `SaveAsync()` → 再判定 → 表示更新 |
| 一覧の星のトグル | 自動生成ルールを追加・削除して即 `SaveAsync()` → 再判定 → 表示更新 |
| メインウィンドウ `Closing` | `Window.Width` / `Height` / `SplitterPosition` を現在値で更新して `SaveAsync()` |

保存の契機が複数あるので、`SaveAsync()` の書き込みは `SemaphoreSlim` で直列化する。

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

コンテンツタイプ 1 つにつき 1 件。同じ `ContentType` を持つ項目は 2 つ作れない。

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `ContentType` | string | `""` | ✔ | 担当するタイプ。`""` は「その他」（どのタイプにも当てはまらない場合の受け皿） |
| `ExecutablePath` | string | `""` | ✔ | 実行ファイルのパス |
| `ArgumentTemplate` | string | `"\"{stream}\""` | ✔ | 引数。置換子は下記 |

指定できるタイプは `FLV` / `MKV` / `WMV` / `WMA` / `OGG` / `OGV` / `MP3` / `AAC` / `NSV` / `RAW` と「その他」
（`PlayerContentTypes.Known`）。チャンネルの `ChannelType` との照合は大文字小文字を区別しない。

#### 引数の置換子（`PlayerPlaceholders`）

| 置換子 | 値 |
|---|---|
| `{stream}` | `StreamUrl` |
| `{channelname}` | `ChannelName` |
| `{contact}` | `ContactUrl` |
| `{genre}` | `Genre` |
| `{description}` | `Description` |
| `{comment}` | `Comment` |
| `{contenttype}` | `ChannelType` |
| `{direct}` | `IsDirect` を `1` / `0` で |

- 表にない語（`{foo}` など）は書かれたまま残る。プレイヤー自身が波かっこを使う記法を持つことがあり、
  空文字に潰すと引数の数が変わってしまうため
- `{url}` は `{stream}` の旧名。読み込み時に書き換わるが、置換自体は引き続き受け付ける

#### 旧形式（名前 +「既定」フラグ）からの移行

`Name` / `IsDefault` が書かれていれば旧形式。旧形式にはタイプの情報が無いため、
**既定のプレイヤー（無ければ先頭）1 件だけ**を「その他」として引き継ぎ、残りは捨てる。
引き継いだ引数の `{url}` は `{stream}` に書き換える。
移行後は `Name` / `IsDefault` が保存ファイルから消える。

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
| `NotifyOnFavorite` | bool | `true` | ✔ | false で通知タグの新着通知を出さない（タグごとの `Notify` より優先） |
| `SoundEnabled` | bool | `false` | ✘ | 未適用（UI にも無い） |
| `SoundFile` | string | `""` | ✘ | 未適用（UI にも無い） |
| `BalloonTimeoutSeconds` | int | `5` | ✘ | 未適用（UI では編集可） |

`NotifyOnFavorite` は旧 `Behavior.NotifyOnFavorite`。タグ機能に吸収されるまでの暫定項目。

### 3.9 `Behavior`（`BehaviorSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `RefreshIntervalSeconds` | int | `60` | ✔ | 自動更新間隔。`60` / `30` / `120` / `0`（更新しない）のみ。下限 30 秒 |
| `StartupState` | enum | `Normal` | ✔ | `Normal` / `Minimized` / `Tray` |
| `MinimizeButtonAction` | enum | `KeepInTaskbar` | ✔ | `KeepInTaskbar` / `MinimizeToTray` |
| `ActiveFilterIndex` | int | `0` | ✘ | 未適用。ビュー選択は保存も復元もされず、起動時は常に「すべて」 |

閉じるボタン（✕）の挙動は「終了」で固定。設定項目は持たない。
最小化とクローズの両方に格納オプションがあると、どちらを設定したのか分からなくなるため。

ダブルクリック再生は常に有効（旧 `OpenOnDoubleClick` は廃止）。

#### 旧「動作」設定からの移行

読み込み時に一度だけ変換され、旧キーは保存ファイルから消える
（値が入っていれば「設定ファイルに書かれていた」= 移行対象、という判定）。

| 旧キー | 新しい形 |
|---|---|
| `StartMinimized = true` | `StartupState = Minimized` |
| `StartMinimized = false` | `StartupState = Normal`（既定のまま） |
| `MinimizeToTray = true` | `MinimizeButtonAction = MinimizeToTray` |
| `MinimizeToTray = false` | `MinimizeButtonAction = KeepInTaskbar` |
| `NotifyOnFavorite` | `Notifications.NotifyOnFavorite` へそのまま |
| `OpenOnDoubleClick` | 破棄（常にオン） |

`RefreshIntervalSeconds` がプリセットに無い場合は最も近い値へ丸める。

| 保存されていた値 | 丸めた結果 |
|---|---|
| `0` 以下 | `0`（更新しない） |
| 正の値 | `30` / `60` / `120` のうち最も近い値。等距離なら短い方 |

正の値が `0` へ丸まることはない。速く更新したかった設定が「更新しない」に化けると、
ユーザーには故障に見えるため。

### 3.10 `Window`（`WindowSettings`）

| キー | 型 | 既定値 | 適用 | 内容 |
|---|---|---|---|---|
| `Width` | double | `900` | ✔ | 起動時に復元（`> 0` のとき）、終了時に保存 |
| `Height` | double | `600` | ✔ | 同上（`Width > 0` のときに一緒に復元） |
| `X` | double | `-1` | ✘ | 保存も復元もされない |
| `Y` | double | `-1` | ✘ | 保存も復元もされない |
| `SplitterPosition` | double | `150` | ✔ | 詳細パネルの高さ。起動時に復元（`> 0` のとき）、終了時に保存 |

## 4. 設定の反映と保存のタイミング

設定ダイアログは **OK・キャンセル**で確定する。
編集の対象は `AppSettings` 本体ではなく、ダイアログを開いた時点で取った複製
（`SettingsDraft`）。本体に書き戻すのは「OK」のときだけ。

```
設定ボタン
   └─ SettingsDraft.From(Current)   ← 複製を取る
        ├─ 各ページはこの複製を読み書きする
        ├─ OK       → Draft.ApplyTo(Current) → SaveAsync()
        └─ キャンセル → 複製を捨てる（本体は最初から触っていない）
```

| 複製に含む | 複製に含まない |
|---|---|
| `YpServers` / `Players` / `AutoDownloadRules` / `Downloader` / `Network` / `Display` / `Notifications` / `Behavior` | `Tags` / `Rules` / `Window` |

`Tags` と `Rules` はルール編集・タグ設定ダイアログが持ち主なので触らない。
設定ダイアログの OK で書き戻す対象にも入れていない（別ダイアログの編集を上書きしないため）。

キャンセルはどのページの変更も残さない。以前は一覧の増減しか戻らず、各項目の中身や
表示・ネットワーク・通知ページの変更は本体に残っていた（[design/decisions.md](../design/decisions.md#なぜ設定ダイアログを複製の編集にしたか)）。

テキスト入力は既定どおりフォーカスが外れた時点で複製へ届く。
入力欄にカーソルを置いたまま OK を押しても値が落ちないよう、
OK では先に現在の入力欄の束縛を確定させてから書き戻す。

## 5. 設定 UI の入力反映パターン

一覧を持つページ（YP サーバー・自動ダウンロードルール）のコードビハインドは
「リスト選択 → テキストボックスへ反映 → 入力 → 複製のオブジェクトへ書き戻し」
という流れを取る。反映時の `TextChanged` で値が上書きされるのを防ぐため `_loading` フラグを使う。

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

数値入力（タイムアウト等）は `TryParse` に成功したときだけ複製へ反映され、
失敗した入力は無視される（エラー表示はない）。
自動更新間隔だけは自由入力をやめてプリセットのコンボボックスにしたので、この問題は起きない。

## 6. 設定 UI の部品

| 部品 | 置き場所 | 役割 |
|---|---|---|
| `SettingCard` | `Views/Controls/SettingCard.cs` | 1 設定 = 1 行。アイコン / 見出し / 説明 / 右のコントロール |
| `SettingsDraft` | `Settings/SettingsDraft.cs` | ダイアログが編集する複製。OK で本体へ書き戻す |
| 見た目の定義 | `Themes/Settings.xaml` | カード・左ナビ・トグルスイッチ・グループ枠のスタイル |

カード行は全ページで同じコントロールを使う。ページごとに手書きすると、間隔と右端の位置が
ページ間でずれて、結局また雑然とした印象に戻るため。
