using System.Windows;

namespace YPBrowser.Abstractions;

/// <summary>
/// タスクトレイの常駐アイコン。
/// アイコンは「ウィンドウをトレイに格納している間だけ」出す。ウィンドウが見えているのに
/// アイコンも出ていると、同じアプリの入口が 2 つあるように見えるため。
/// </summary>
public interface ITrayIconService
{
    /// <summary>今トレイにアイコンが出ているか。</summary>
    bool IsVisible { get; }

    /// <summary>
    /// メッセージの受け口にするウィンドウを渡す。アイコンを出す前に 1 回だけ呼ぶ。
    /// </summary>
    void Attach(Window window);

    void Show();
    void Hide();

    /// <summary>アイコンにかざしたときの説明。出ていないときは何もしない。</summary>
    void UpdateTooltip(int channelCount, int totalListeners);

    /// <summary>アイコンのクリック、またはメニューの「表示」。</summary>
    event EventHandler? ShowWindowRequested;

    /// <summary>メニューの「終了」。</summary>
    event EventHandler? ExitRequested;
}
