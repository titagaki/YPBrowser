# YPBrowser

PeerCast Yellow Pages ブラウザ。Delphi製 `pcypLite` を C# + WinUI 3 で再実装したもの。

## プロジェクト構造

```
src/YPBrowser/          # メインアプリ (WinUI 3, net9.0-windows10.0.19041.0)
tests/YPBrowser.Tests/  # xUnit テスト (net9.0)
_ref/pcypLite/          # 参照元 Delphi ソース (gitignore済み)
```

## ビルド・テスト

```bash
# ビルド (x64)
dotnet build src/YPBrowser/YPBrowser.csproj -p:Platform=x64

# テスト
dotnet test tests/YPBrowser.Tests/

# ソリューション全体
dotnet build YPBrowser.sln -p:Platform=x64
```

## アーキテクチャ

- **パターン**: MVVM (CommunityToolkit.Mvvm)
- **DI**: `App.xaml.cs` で `Microsoft.Extensions.DependencyInjection`
- **設定**: JSON (`%AppData%\YPBrowser\settings.json`)
- **通知**: Windows トースト (`Windows.UI.Notifications`)
- **プレイヤー**: 外部プレイヤーをURL/プレイリストで起動 (`Process.Start`)

## 主要クラス

| クラス | 役割 |
|---|---|
| `YpFetchService` | HTTP取得 + パース |
| `ChannelDiffService` | 差分計算 (Up/Down/New/Log) |
| `FavoriteMatchService` | 正規表現マッチング + 色設定 |
| `AutoRefreshService` | 定期更新 (PeriodicTimer) |
| `MainViewModel` | UI オーケストレーター |
| `SettingsService` | JSON 設定の読み書き |

## ドキュメント

設計背景・落とし穴・類似概念の使い分けは [`docs/README.md`](docs/README.md) を参照。
YP データ形式（19フィールド仕様・パース・フィルタリング）は [`docs/yp-data-format.md`](docs/yp-data-format.md) を参照。

## 注意事項

- `[ObservableProperty]` フィールドに MVVMTK0045 警告が多数出るが、アンパッケージドWinUI 3では問題なし
