using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EcclesiaCast.App.Views;

/// <summary>A themed color picker: preview + hex, preset swatches, and RGB sliders.</summary>
public partial class ColorPickerWindow : Window
{
    private static readonly string[] Presets =
    [
        "#FFFFFF", "#000000", "#F2F2F2", "#B9C6DE", "#9AA1B2", "#5B6070",
        "#2F63C9", "#7CA4EF", "#1E88E5", "#00B4D8", "#2E7D4F", "#6FBF8F",
        "#E8C34A", "#F2A93B", "#C43B3B", "#E06666", "#8E44AD", "#D264C8",
    ];

    /// <summary>Returned when the operator picks "Transparent".</summary>
    public const string Transparent = "TRANSPARENT";

    private bool _loading;

    public ColorPickerWindow(string? currentHex, bool allowTransparent = false)
    {
        InitializeComponent();
        Swatches.ItemsSource = Presets;
        TransparentButton.Visibility = allowTransparent ? Visibility.Visible : Visibility.Collapsed;

        var color = ParseOr(currentHex, Colors.White);
        _loading = true;
        RSlider.Value = color.R;
        GSlider.Value = color.G;
        BSlider.Value = color.B;
        _loading = false;
        SyncFromRgb();
    }

    public string ResultHex { get; private set; } = "#FFFFFF";

    private static Color ParseOr(string? hex, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex ?? "");
        }
        catch
        {
            return fallback;
        }
    }

    private void Rgb_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
            return;
        SyncFromRgb();
    }

    private void SyncFromRgb()
    {
        var color = Color.FromRgb((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value);
        RValue.Text = $"{color.R}";
        GValue.Text = $"{color.G}";
        BValue.Text = $"{color.B}";
        Preview.Background = new SolidColorBrush(color);

        _loading = true;
        HexBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        _loading = false;
        ResultHex = HexBox.Text;
    }

    private void Hex_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(HexBox.Text.Trim());
            _loading = true;
            RSlider.Value = color.R;
            GSlider.Value = color.G;
            BSlider.Value = color.B;
            _loading = false;
            SyncFromRgb();
        }
        catch
        {
            // Texto incompleto/ inválido: se ignora hasta que sea un hex válido.
        }
    }

    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string hex })
        {
            var color = ParseOr(hex, Colors.White);
            _loading = true;
            RSlider.Value = color.R;
            GSlider.Value = color.G;
            BSlider.Value = color.B;
            _loading = false;
            SyncFromRgb();
        }
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        ResultHex = HexBox.Text.Trim();
        DialogResult = true;
    }

    private void Transparent_Click(object sender, RoutedEventArgs e)
    {
        ResultHex = Transparent;
        DialogResult = true;
    }
}
