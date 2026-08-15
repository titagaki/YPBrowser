using System.Windows;
using System.Windows.Media;
using YPBrowser.Helpers;

namespace YPBrowser.Views;

/// <summary>
/// タグの色を選ぶ。パレット / RGB スライダー / 16 進のどれでも指定でき、結果をその場でプレビューする。
/// </summary>
public partial class ColorPickerDialog : Window
{
    private static readonly string[] Palette =
    [
        "#FFFFFF", "#F0F0F0", "#D1D1D1", "#8A8A8A", "#4A4A4A", "#1B1B1B",
        "#FFF4CE", "#FFD45E", "#FFB900", "#D98F00", "#8A5A00", "#4A3A00",
        "#E6F4EA", "#A8DCBD", "#4CAF7D", "#0F7B0F", "#0B5D2A", "#0B3D24",
        "#EFF6FC", "#CFE3F5", "#5BA3E0", "#0067C0", "#00457D", "#003E73",
        "#FDF2F2", "#FADCD9", "#E88C82", "#C42B1C", "#8B1F14", "#6B1710",
        "#F5EEFB", "#E0CCF0", "#B07FD8", "#7A3FA8", "#54276F", "#3A1B4D",
    ];

    /// <summary>OK で閉じたときに選ばれた色（`#RRGGBB`）。</summary>
    public string? SelectedHex { get; private set; }

    private bool _updating;

    public ColorPickerDialog(string? initial)
    {
        InitializeComponent();

        Swatches.ItemsSource = Palette
            .Select(hex => new { Hex = hex, Brush = new SolidColorBrush(ColorHelper.Parse(hex)!.Value) })
            .ToList();

        var color = ColorHelper.Parse(initial) ?? Colors.White;
        SetColor(color);
    }

    private void SetColor(Color color)
    {
        _updating = true;
        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        HexBox.Text = ColorHelper.ToHex(color);
        _updating = false;
        UpdatePreview(color);
    }

    private void UpdatePreview(Color color) => Preview.Background = new SolidColorBrush(color);

    private void Channel_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || HexBox is null) return;

        var color = CurrentSliderColor();
        _updating = true;
        HexBox.Text = ColorHelper.ToHex(color);
        _updating = false;
        UpdatePreview(color);
    }

    private void HexBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_updating) return;
        if (ColorHelper.Parse(HexBox.Text) is not { } color) return;

        _updating = true;
        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        _updating = false;
        UpdatePreview(color);
    }

    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string hex) return;
        if (ColorHelper.Parse(hex) is { } color) SetColor(color);
    }

    private Color CurrentSliderColor() => Color.FromRgb(
        (byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        SelectedHex = ColorHelper.ToHex(
            ColorHelper.Parse(HexBox.Text) ?? CurrentSliderColor());
        DialogResult = true;
        Close();
    }
}
