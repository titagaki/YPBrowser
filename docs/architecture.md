# アーキテクチャ概要

## なぜ MVVM + DI か

元の Delphi 製 pcypLite はシングルフォームに処理が集中していた。
C# 移植時に以下の理由から MVVM + DI を採用した。

- **テスタビリティ**: 各 Service を Interface 化し、モック差し替えを可能にする
- **責務分離**: UI (View) とビジネスロジック (ViewModel/Service) を分離する
- **拡張性**: 将来の YP 形式追加や UI 変更に対応しやすくする

## レイヤー構造

```
Views/          ← WPF XAML + コードビハインド（UIイベント処理のみ）
ViewModels/     ← コマンド・状態管理（UIロジック）
Services/       ← ビジネスロジック（HTTP通信・差分計算・マッチング等）
Models/         ← ドメインモデル（ObservableObject）
Settings/       ← 設定 DTO（普通の POCO）
Helpers/        ← 変換・ユーティリティ
Abstractions/   ← Service Interface 定義
```

> **注意**: `CLAUDE.md` には "WinUI 3" と記述されているが、実際は WPF (`<UseWPF>true</UseWPF>`)。
> 初期の計画が変わった経緯があるため注意。

## WPF 実装上の注意

- **XAML のルート要素は `<Window>`**: `WindowEx` は C# 側のクラス継承でのみ使い、XAML では `<Window>` を使う。`<WindowEx>` をルートにすると XAML デザイナーが壊れる。
- **設定ダイアログは別 `Window`**: `ContentDialog` はサイズが固定・小さく、設定項目が多いページには不向きなため `Window` を別途作成している。

## サービス一覧と単一責任

| インターフェース | 責務 | なぜ分けたか |
|---|---|---|
| `IYpFetchService` | HTTP 取得 + index.txt パース | 通信とパースを他から切り離す |
| `IChannelDiffService` | 更新間の差分検出 + Log 管理 | 差分状態は「セッションをまたいだ記憶」が必要なため Singleton |
| `IFavoriteMatchService` | チャンネルとお気に入りルールのマッチング | 正規表現キャッシュを保持するため Singleton |
| `IAutoDownloadMatchService` | チャンネルと自動ダウンロードルールのマッチング | FavoriteMatchService と同じエンジンだが責務を分離 |
| `IAutoRefreshService` | 定期取得タイマー管理 | タイマーは1つだけ動くべき → Singleton |
| `IChannelFilterService` | 表示フィルタ + 検索 + ソート | 純粋関数に近いが Interface 化でテスト容易に |
| `IPlayerLaunchService` | 外部プレイヤー起動 | Process.Start をラップして差し替え可能に |
| `IRecordService` | HTTP ストリームのファイル保存 | 録音状態（実行中かどうか）を保持するため Singleton |
| `INotificationService` | Windows トースト通知 | OS API を隔離 |
| `ISettingsService` | JSON 設定の読み書き | 設定は単一の真実 (Single Source of Truth) |

## DI ライフタイムの使い分け

```
Singleton  ← 状態を持つ、またはアプリ全体で1インスタンス必要なもの
Transient  ← ダイアログを開くたびに新規作成したいもの
```

### Singleton にしている理由

- `IChannelDiffService`: Log チャンネルのキャッシュ（1時間有効）を保持
- `IFavoriteMatchService`, `IAutoDownloadMatchService`: コンパイル済み正規表現キャッシュを保持
- `IAutoRefreshService`: 定期タイマーは多重起動を避けるため必ず1つ
- `IRecordService`: 録音中チャンネルの `CancellationTokenSource` 辞書を保持
- `MainViewModel`: メイン画面は1つだけ存在する

### Transient にしている理由

- `SettingsViewModel`, `FavoritesViewModel`:
  設定ダイアログを開くたびに現在の設定をロードし直す。
  「キャンセル」したときに変更が反映されないよう、ダイアログ側ではローカルコピーを持つ。
  `SaveAsync()` が呼ばれて初めて JSON に書き込まれる（2段階コミット）。

## イベント駆動の更新フロー

```
AutoRefreshService
  │ (定期タイマー / 手動更新)
  ↓ RefreshCompleted イベント
MainViewModel.OnRefreshCompleted()
  │
  ├─ ChannelDiffService.ApplyDiff()     ← New/Up/Down/Log を算出
  ├─ FavoriteMatchService.MatchAll()    ← 色・IsFavorite を設定
  ├─ NotificationService.NotifyNewFavorites()   ← 新着お気に入りをトースト
  ├─ AutoDownloadMatchService (自動録音)        ← 新着マッチを録音開始
  └─ ChannelFilterService.Filter()      ← 画面に表示する一覧を更新
```

`MainViewModel` は「オーケストレーター」であり、各 Service を組み合わせるが
それ自身はロジックを持たない。

## YpFetchService の二重登録パターン

```csharp
// HttpClient の自動注入を使うため TypedClient として登録
services.AddHttpClient<YpFetchService>(...);

// IYpFetchService として解決できるよう、ファクトリで橋渡し
services.AddSingleton<IYpFetchService>(sp => sp.GetRequiredService<YpFetchService>());
```

`AddHttpClient<T>` は T を Transient 扱いするが、上記のように
`GetRequiredService<YpFetchService>()` を Singleton ファクトリで包むことで
実質 Singleton として動く。`HttpClientFactory` によって内部の `HttpMessageHandler`
はプールされるため、Singleton でも接続が適切に再利用される。
