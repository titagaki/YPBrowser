using System.Windows;

namespace YPBrowser.Helpers;

/// <summary>
/// 保存した位置にウィンドウを戻してよいかの判定。
///
/// モニタを外した・解像度を変えた後などに前回の位置をそのまま使うと、
/// ウィンドウが画面の外に出て掴めなくなる。そうなると設定を手で消すまで直せないので、
/// 戻す前にここで確かめる。
/// </summary>
public static class WindowPlacement
{
    /// <summary>
    /// 画面に残っていてほしい最小の大きさ。タイトルバーを掴んで引き戻せる程度あればよい。
    /// </summary>
    private const double MinVisibleWidth = 120;
    private const double MinVisibleHeight = 40;

    /// <summary>
    /// <paramref name="window"/> の位置に戻してよければ true。
    /// <paramref name="virtualScreen"/> は全モニタを覆う矩形。
    /// </summary>
    public static bool IsOnScreen(Rect window, Rect virtualScreen)
    {
        if (window.Width <= 0 || window.Height <= 0) return false;
        if (virtualScreen.IsEmpty) return false;

        var visible = Rect.Intersect(window, virtualScreen);
        if (visible.IsEmpty) return false;

        return visible.Width >= MinVisibleWidth && visible.Height >= MinVisibleHeight;
    }

    /// <summary>いま繋がっているモニタ全体を覆う矩形。</summary>
    public static Rect CurrentVirtualScreen() => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);
}
