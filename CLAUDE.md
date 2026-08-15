# YPBrowser

PeerCast Yellow Pages ブラウザ。

## プロジェクト構造

```
src/YPBrowser/          # メインアプリ (WPF, net9.0-windows10.0.19041.0)
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
| `RecordService` | HTTP ストリーム保存・PLS/M3U 解決・再試行 (最大10回)・進捗追跡 |
| `RecordingEntry` | 録画中エントリの実行時状態 (進捗・リトライ数・IsActive) |

## ドキュメント

- 仕様（何をするか）: [`docs/spec/`](docs/spec/) — 取得・差分・マッチング・録音・設定・UI
- 設計理由（なぜそうしたか）: [`docs/design/`](docs/design/)
- 索引: [`docs/README.md`](docs/README.md)

### ドキュメント運用ルール

- CLAUDE.md には、毎回必ず守るルールだけを書く（目安100行以内）
- 仕様は `docs/spec/` に、設計理由は `docs/design/` に分けて書く（spec に理由を書かない）
- Claude への追加指示は `.claude/rules/` に置く
- 未検証の調査結果は `docs/investigations/` に置き、事実と仮説を明記して分ける。
  検証済みになったら `docs/` へ昇格し、元ファイルは削除する
- 新しい文書を作る前に、`docs/` 配下を検索して重複を確認する
- タスクの進捗状態は `docs/roadmap.md` を更新して管理する

## 注意事項

- `[ObservableProperty]` フィールドに MVVMTK0045 警告が多数出るが、WPF では問題なし
