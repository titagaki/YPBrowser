# 設計: アーキテクチャ

「何をするか」は [`docs/spec/`](../spec/) を参照。ここでは選択の理由だけを書く。

## なぜ MVVM + DI か

- **テスタビリティ**: 各 Service を Interface 化し、モック差し替えを可能にする
- **責務分離**: UI (View) とビジネスロジック (ViewModel/Service) を分離する
- **拡張性**: YP 形式の追加や UI 変更の影響範囲を Service 単位に閉じ込める

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

`MainViewModel` は各 Service を組み合わせる「オーケストレーター」であり、
差分計算やマッチングのロジック自体は持たない。

## WPF 実装上の注意

- **XAML のルート要素は `<Window>`**: `WindowEx` は C# 側のクラス継承でのみ使い、XAML では `<Window>` を使う。`<WindowEx>` をルートにすると XAML デザイナーが壊れる。
- **設定ダイアログは別 `Window`**: `ContentDialog` はサイズが固定・小さく、設定項目が多いページには不向きなため `Window` を別途作成している。

## サービスを分けた理由

| インターフェース | なぜ分けたか |
|---|---|
| `IYpFetchService` | 通信とパースを他の関心事から切り離す |
| `IChannelDiffService` | 差分状態は「前回との比較」の記憶が必要 → Singleton にする必然がある |
| `IFavoriteMatchService` | 正規表現キャッシュを保持するため Singleton |
| `IAutoDownloadMatchService` | マッチエンジンは共通だが、色付け（表示）と録音開始（副作用）は責務が別 |
| `IAutoRefreshService` | タイマーは 1 つだけ動くべき → Singleton |
| `IChannelFilterService` | 純粋関数に近いが、Interface 化してテストしやすくする |
| `IPlayerLaunchService` | `Process.Start` をラップして差し替え可能にする |
| `IRecordService` | 録音中の `CancellationTokenSource` を保持するため Singleton |
| `INotificationService` | OS API（トースト）を隔離する |
| `ISettingsService` | 設定を単一の真実 (Single Source of Truth) にする |

## DI ライフタイムの使い分け

```
Singleton  ← 状態を持つ、またはアプリ全体で 1 インスタンス必要なもの
Transient  ← ダイアログを開くたびに現在値から作り直したいもの
```

### Singleton の理由（保持している状態）

| サービス | 状態 |
|---|---|
| `ChannelDiffService` | YP 別のログキャッシュ（1 時間有効） |
| `FavoriteMatchService` / `AutoDownloadMatchService` | コンパイル済み正規表現キャッシュ |
| `AutoRefreshService` | 定期タイマー |
| `RecordService` | 録音中チャンネルの CTS 辞書 |
| `MainViewModel` | メイン画面は 1 つだけ |

これらを Transient にすると、ログが消える・正規表現が毎回コンパイルされる・
タイマーが多重起動する、といった問題が起きる。

### Transient の理由

`SettingsViewModel`, `FavoritesViewModel` はダイアログを開くたびに現在の設定を読み直したい。
「キャンセルで一覧の編集を捨てる」挙動も、ViewModel 側の `ObservableCollection` にだけ
追加・削除・並べ替えを溜め、OK 時に `AppSettings` へ差し替えることで実現している。

ただしこの方式で守られるのは**一覧の構成だけ**で、各項目の中身は同じインスタンスを
共有しているためキャンセルしても戻らない。現状の制約（[decisions.md](decisions.md#設定のキャンセルが一部しか効かない)）。

## YpFetchService の二重登録パターン

```csharp
// HttpClient の自動注入を使うため TypedClient として登録
services.AddHttpClient<YpFetchService>(...);

// IYpFetchService として解決できるよう、ファクトリで橋渡し
services.AddSingleton<IYpFetchService>(sp => sp.GetRequiredService<YpFetchService>());
```

`AddHttpClient<T>` は T を Transient 扱いするが、`GetRequiredService<YpFetchService>()` を
Singleton ファクトリで包むことで実質 Singleton として動く。
`HttpClientFactory` が内部の `HttpMessageHandler` をプールするため、
Singleton でも接続は適切に再利用される。

録音用の `HttpClient` は名前付きクライアント (`"RecordService"`) にして、
タイムアウト無限大という特殊な設定を YP 取得側と混ぜないようにしている。

## イベント駆動の更新フロー

```
AutoRefreshService
  │ (定期タイマー / 手動更新)
  ↓ RefreshCompleted イベント
MainViewModel.OnRefreshCompleted()
  │
  ├─ ChannelDiffService.ApplyDiff()           ← New/Up/Down/Log を算出
  ├─ FavoriteMatchService.MatchAll()          ← 色・IsFavorite/IsNG を設定
  ├─ NotificationService.NotifyNewFavorites() ← 新着お気に入りをトースト
  ├─ AutoDownloadMatchService → RecordService ← 新着マッチを録音開始
  └─ ChannelFilterService.Filter()            ← 画面に表示する一覧を更新
```

Service 側はイベントを投げるだけで UI を知らない。UI スレッドへの切り替え（`Dispatcher.BeginInvoke`）は
`MainViewModel` が一手に引き受ける。
