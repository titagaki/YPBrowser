# YPBrowser

PeerCast Yellow Pages ブラウザ。

## プロジェクト構造

```
src/YPBrowser/          # メインアプリ (WPF, net9.0-windows10.0.19041.0)
tests/YPBrowser.Tests/  # xUnit テスト (net9.0)
```

仕様は自分たちで決める。他アプリの真似をする前提では書かない。

## ビルド・テスト

```bash
# ビルド
dotnet build src/YPBrowser/YPBrowser.csproj

# テスト
dotnet test tests/YPBrowser.Tests/

# ソリューション全体
dotnet build YPBrowser.sln
```

`-p:Platform=` は付けない。どちらの csproj も `Platforms` を宣言しておらず、
渡した値の分だけ `bin/` にディレクトリが増えるだけで、意味のある指定にならない。

## アーキテクチャ

- **パターン**: MVVM (CommunityToolkit.Mvvm)
- **DI**: `App.xaml.cs` で `Microsoft.Extensions.DependencyInjection`
- **設定**: JSON (`%AppData%\YPBrowser\settings.json`)
- **通知**: Windows トースト (`Windows.UI.Notifications`)
- **トレイ**: `Shell_NotifyIcon` を直接呼ぶ (WinForms は暗黙 using が WPF と衝突するため使わない)
- **プレイヤー**: コンテンツタイプごとに外部プレイヤーを起動 (`Process.Start`)

## 主要クラス

| クラス | 役割 |
|---|---|
| `YpFetchService` | HTTP取得 + パース |
| `ChannelDiffService` | 差分計算 (Up/Down/New/Log) |
| `TagMatchService` | ルール評価 → チャンネルへタグ付与 (表示は決めない) |
| `TagDefinition` / `Rule` | タグ (色・既定の扱い・通知) / 判定ルール (条件 → タグID) |
| `ChannelFilterService` | ビュー + 絞り込み + 非表示件数 |
| `SettingsMigration` | 旧 `Favorites` → タグ方式 / 旧プレイヤー → タイプごとへの移行 |
| `PlayerPlaceholders` | 引数テンプレートの置換子と展開（説明と挙動の唯一の出どころ） |
| `PlayerSelection` | チャンネルのタイプ → 使うプレイヤー（無ければ「その他」） |
| `AutoRefreshService` | 定期更新 (PeriodicTimer) |
| `TrayIconService` | トレイ常駐アイコン (`Shell_NotifyIcon` の P/Invoke) |
| `MainViewModel` | UI オーケストレーター |
| `SettingsService` | JSON 設定の読み書き |
| `RecordService` | HTTP ストリーム保存・PLS/M3U 解決・再試行 (最大10回)・進捗追跡 |
| `RecordingEntry` | 録画中エントリの実行時状態 (進捗・リトライ数・IsActive) |
| `IRecordingSink` | 録画データの書き込み先。コンテナ形式ごとの後始末をここに閉じ込める |
| `FlvRecordingSink` | FLV を単体再生できる形に組み直す (ts を録画開始基準へ・再送ヘッダ除去・onMetaData 差し替え) |

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
- ルールはタグを **ID** で参照する（名前で参照しない。改名で壊れるため）
- 星が作るルールは必ず `Exact`。`Regex` にするとチャンネル名のメタ文字で誤爆する
