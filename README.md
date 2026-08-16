# YPBrowser

PeerCast の Yellow Pages（YP）に出ている配信チャンネルを一覧で見るための Windows アプリ。

## できること

- YP からチャンネル一覧を定期的に取得して表示する
- 前回との差分（新着 / 視聴者増 / 視聴者減 / 配信終了）を色で見分ける
- 条件（チャンネル名・ジャンル・説明など）に合う配信へ**タグ**を付ける
  - タグごとに色・通知・「一覧から隠す」を設定できる
  - 星を押すとお気に入りタグが付く
- タグやテキストで一覧を絞り込む
- 外部プレイヤーで再生する（VLC など、実行ファイルと引数を設定）
- 配信を録音・録画してファイルに保存する（切断時は自動で再試行）
- 気になる配信が始まったら Windows のトースト通知で知らせる

## 動作環境

- Windows 10 (1809) 以降
- [.NET 9 デスクトップランタイム](https://dotnet.microsoft.com/download/dotnet/9.0)

## ビルドと実行

```bash
# ビルド
dotnet build src/YPBrowser/YPBrowser.csproj -p:Platform=x64

# 実行
dotnet run --project src/YPBrowser/YPBrowser.csproj

# テスト
dotnet test tests/YPBrowser.Tests/
```

## 設定の保存場所

```
%AppData%\YPBrowser\settings.json
```

YP のアドレス、プレイヤー、タグとルール、通知、録画の保存先などはすべてここに入る。
アプリ内の「設定」「ルール」「タグ」から編集する。

## ドキュメント

開発者向けの資料は [docs/](docs/) にある。

| | |
|---|---|
| [docs/spec/](docs/spec/) | 仕様（何をするか） |
| [docs/design/](docs/design/) | 設計（なぜそうしたか） |
| [docs/roadmap.md](docs/roadmap.md) | 進捗と残タスク |
