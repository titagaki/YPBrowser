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

## YP データ形式

`index.txt` の各行を `<>` で分割した19フィールド:
```
チャンネル名<>ID<>Host:Port<>ContactURL<>ジャンル<>説明<>視聴者数<>リレー数
<>Kbps<>コーデック<>アーティスト<>アルバム<>曲名<>曲ジャンル<>URLParam
<>放送時間<>きゃすこステータス<>コメント<>isDirect(0/1)
```

実装参照: `_ref/pcypLite/upcypNet.pas` の `ParseTxt` メソッド

## 主要クラス

| クラス | 役割 |
|---|---|
| `YpFetchService` | HTTP取得 + パース |
| `ChannelDiffService` | 差分計算 (Up/Down/New/Log) |
| `FavoriteMatchService` | 正規表現マッチング + 色設定 |
| `AutoRefreshService` | 定期更新 (PeriodicTimer) |
| `MainViewModel` | UI オーケストレーター |
| `SettingsService` | JSON 設定の読み書き |

## 注意事項

- `[ObservableProperty]` フィールドに MVVMTK0045 警告が多数出るが、アンパッケージドWinUI 3では問題なし
- XAML のルート要素は `<Window>` (WindowEx は C# 側だけで継承)
- 設定ダイアログは別 `Window` として実装 (ContentDialog では狭いため)
- YPサーバーの最小フェッチ間隔: 4分/サーバー (AutoRefreshService)
