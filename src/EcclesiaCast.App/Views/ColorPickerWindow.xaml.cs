using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.App.Views;

/// <summary>
/// A themed color picker: the color chosen with its code, a big palette (tones
/// by lightness plus greys), the colors used lately, and a fine adjustment
/// with hue, intensity and lightness.
/// </summary>
public partial class ColorPickerWindow : Window
{
    /// <summary>Hues of the palette columns, in degrees.</summary>
    private static readonly double[] Hues = [0, 25, 50, 90, 150, 185, 215, 255, 285, 325];

    /// <summary>Saturation and value of each palette row, from light to dark.</summary>
    private static readonly (double S, double V)[] Rows =
    [
        (0.25, 1.0), (0.5, 1.0), (0.8, 1.0), (1.0, 0.85), (1.0, 0.62), (1.0, 0.4),
    ];

    /// <summary>Returned when the operator picks "Transparent".</summary>
    public const string Transparent = "TRANSPARENT";

    private bool _loading;

    public ColorPickerWindow(string? currentHex, bool allowTransparent = false)
    {
        _loading = true; // the sliders raise ValueChanged while the window is being built
        InitializeComponent();
        TransparentButton.Visibility = allowTransparent ? Visibility.Visible : Visibility.Collapsed;

        BuildPalette();
        BuildRecent();
        SetColor(ParseOr(currentHex, Colors.White));
    }

    public string ResultHex { get; private set; } = "#FFFFFF";

    private void BuildPalette()
    {
        // Rows run light to dark, one column per hue.
        foreach (var (s, v) in Rows)
        {
            foreach (var hue in Hues)
            {
                var (r, g, b) = ColorMath.HsvToRgb(hue, s, v);
                PaletteGrid.Children.Add(MakeSwatch($"#{r:X2}{g:X2}{b:X2}"));
            }
        }

        // The greys, from white to black.
        for (var i = 0; i < 10; i++)
        {
            var level = (byte)Math.Round(255 - i * 255d / 9);
            PaletteGrid.Children.Add(MakeSwatch($"#{level:X2}{level:X2}{level:X2}"));
        }
    }

    private void BuildRecent()
    {
        var recent = RecentColors.Parse(ColorPickerHelper.Settings?.Get(RecentColors.SettingKey));
        RecentGrid.Children.Clear();
        foreach (var hex in recent)
            RecentGrid.Children.Add(MakeSwatch(hex));
        RecentPanel.Visibility = recent.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Button MakeSwatch(string hex)
    {
        var button = new Button
        {
            Height = 24,
            Margin = new Thickness(2),
            MinWidth = 24,
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = hex,
            ToolTip = hex,
            Template = SwatchTemplate,
            Background = new SolidColorBrush(ParseOr(hex, Colors.White)),
        };
        AutomationProperties_SetName(button, hex);
        button.Click += Swatch_Click;
        return button;
    }

    private static void AutomationProperties_SetName(DependencyObject element, string name)
        => System.Windows.Automation.AutomationProperties.SetName(element, "Color " + name);

    private ControlTemplate SwatchTemplate { get; } = BuildSwatchTemplate();

    private static ControlTemplate BuildSwatchTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty,
            new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }

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

    /// <summary>Puts a color everywhere: preview, code and the three fine sliders.</summary>
    private void SetColor(Color color, bool keepSliders = false)
    {
        _loading = true;
        Preview.Background = new SolidColorBrush(color);
        HexBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        ResultHex = HexBox.Text;

        if (!keepSliders)
        {
            var (h, s, v) = ColorMath.RgbToHsv(color.R, color.G, color.B);
            HueSlider.Value = h;
            SatSlider.Value = s * 100;
            ValSlider.Value = v * 100;
        }

        HueValue.Text = $"{(int)HueSlider.Value}";
        SatValue.Text = $"{(int)SatSlider.Value}";
        ValValue.Text = $"{(int)ValSlider.Value}";
        _loading = false;
    }

    private void Hsv_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
            return;

        var (r, g, b) = ColorMath.HsvToRgb(HueSlider.Value, SatSlider.Value / 100, ValSlider.Value / 100);
        SetColor(Color.FromRgb(r, g, b), keepSliders: true);
    }

    private void Hex_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(HexBox.Text.Trim());
            SetColor(color);
        }
        catch
        {
            // Texto incompleto/ inválido: se ignora hasta que sea un hex válido.
        }
    }

    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string hex })
            SetColor(ParseOr(hex, Colors.White));
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        ResultHex = HexBox.Text.Trim();
        Remember(ResultHex);
        DialogResult = true;
    }

    /// <summary>Keeps the picked color in the "used lately" row for next time.</summary>
    private static void Remember(string hex)
    {
        var settings = ColorPickerHelper.Settings;
        if (settings is null)
            return;

        try
        {
            var list = RecentColors.Add(RecentColors.Parse(settings.Get(RecentColors.SettingKey)), hex);
            settings.Set(RecentColors.SettingKey, RecentColors.Serialize(list));
        }
        catch (Exception ex)
        {
            // Not being able to remember a color must never stop the pick.
            Serilog.Log.Warning(ex, "The recent colors could not be saved");
        }
    }

    private void Transparent_Click(object sender, RoutedEventArgs e)
    {
        ResultHex = Transparent;
        DialogResult = true;
    }
}
