using System.Windows;
using YPBrowser.Helpers;

namespace YPBrowser.Tests;

/// <summary>
/// 前回の位置に戻してよいかの判定。
/// モニタ構成が変わった後に、掴めない場所へウィンドウを出さないためのもの。
/// </summary>
public class WindowPlacementTests
{
    /// <summary>1920x1080 が 1 枚だけの構成。</summary>
    private static readonly Rect SingleMonitor = new(0, 0, 1920, 1080);

    /// <summary>左側にもう 1 枚ある構成。左のモニタの座標は負になる。</summary>
    private static readonly Rect DualMonitor = new(-1920, 0, 3840, 1080);

    [Fact]
    public void FullyInside_IsOnScreen()
    {
        Assert.True(WindowPlacement.IsOnScreen(new Rect(100, 100, 900, 600), SingleMonitor));
    }

    /// <summary>左側のサブモニタに置いていた場合。負の座標を弾いてはいけない。</summary>
    [Fact]
    public void OnALeftHandMonitor_IsOnScreen()
    {
        Assert.True(WindowPlacement.IsOnScreen(new Rect(-1800, 100, 900, 600), DualMonitor));
    }

    /// <summary>そのモニタを外した後は同じ位置が画面外になる。</summary>
    [Fact]
    public void OnAMonitorThatIsGone_IsNotOnScreen()
    {
        Assert.False(WindowPlacement.IsOnScreen(new Rect(-1800, 100, 900, 600), SingleMonitor));
    }

    [Theory]
    [InlineData(-899, 100)]      // 右端 1px だけ残る
    [InlineData(1919, 100)]      // 左端 1px だけ残る
    [InlineData(100, 1079)]      // 上端 1px だけ残る
    [InlineData(100, -599)]      // 下端 1px だけ残る
    public void BarelyOverlapping_IsNotOnScreen(double x, double y)
    {
        Assert.False(WindowPlacement.IsOnScreen(new Rect(x, y, 900, 600), SingleMonitor));
    }

    /// <summary>掴める分だけ残っていれば戻してよい。</summary>
    [Fact]
    public void PartiallyOffScreenButGrabbable_IsOnScreen()
    {
        // 右へはみ出して 200px 残る
        Assert.True(WindowPlacement.IsOnScreen(new Rect(1720, 100, 900, 600), SingleMonitor));
    }

    [Fact]
    public void EmptyOrDegenerateInput_IsNotOnScreen()
    {
        Assert.False(WindowPlacement.IsOnScreen(new Rect(100, 100, 0, 600), SingleMonitor));
        Assert.False(WindowPlacement.IsOnScreen(new Rect(100, 100, 900, 0), SingleMonitor));
        Assert.False(WindowPlacement.IsOnScreen(new Rect(100, 100, 900, 600), Rect.Empty));
    }
}
