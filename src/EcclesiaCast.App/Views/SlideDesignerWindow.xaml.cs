using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Views;

/// <summary>
/// ProPresenter-style designer for one slide: a draggable/resizable text box
/// over the 1920×1080 canvas (shown at half scale) plus per-slide format
/// overrides. Everything not touched keeps following the theme.
/// </summary>
public partial class SlideDesignerWindow : Window
{
    private const double Scale = 0.5;
    private const double CanvasW = 960;
    private const double CanvasH = 540;

    private readonly SlideTheme _theme;
    private bool _loading = true;
    private bool _dragging;
    private Point _dragOffset;

    public SlideDesignerWindow(string text, SlideTheme theme, SlideOverride? current)
    {
        InitializeComponent();
        _theme = theme;

        ApplyBackground(theme);
        BoxText.Text = theme.Uppercase ? text.ToUpperInvariant() : text;
        BoxText.FontFamily = new FontFamily(theme.FontFamily);

        // Caja inicial: la del override, o el área que definen los márgenes del tema.
        double x, y, w, h;
        if (current?.HasBox == true)
        {
            x = current.BoxX!.Value * Scale;
            y = current.BoxY!.Value * Scale;
            w = current.BoxWidth!.Value * Scale;
            h = current.BoxHeight!.Value * Scale;
        }
        else
        {
            x = theme.MarginHorizontal * Scale;
            y = theme.MarginVertical * Scale;
            w = (1920 - 2 * theme.MarginHorizontal) * Scale;
            h = (1080 - 2 * theme.MarginVertical) * Scale;
        }
        SetBox(x, y, w, h);

        FontSizeSlider.Value = current?.FontSize ?? theme.MaxFontSize;
        BoldCheck.IsChecked = current?.Bold ?? theme.Bold;
        ItalicCheck.IsChecked = current?.Italic ?? theme.Italic;
        ColorBox.Text = current?.TextColor ?? theme.TextColor;
        AlignHCombo.SelectedIndex = (int)(current?.AlignH ?? theme.AlignH);
        AlignVCombo.SelectedIndex = (int)(current?.AlignV ?? theme.AlignV);

        _loading = false;
        UpdateBoxText();
    }

    public bool Saved { get; private set; }
    public SlideOverride? Result { get; private set; }

    // ── Fondo del lienzo (según el tema) ─────────────────────────

    private void ApplyBackground(SlideTheme theme)
    {
        try
        {
            BackgroundHost.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(theme.BackgroundColor));
        }
        catch
        {
            // Color inválido: queda el fondo por defecto.
        }

        if (!string.IsNullOrWhiteSpace(theme.BackgroundImagePath) && File.Exists(theme.BackgroundImagePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(theme.BackgroundImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 960;
                bitmap.EndInit();
                bitmap.Freeze();
                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
            }
            catch
            {
                // Imagen ilegible: solo color.
            }
        }

        DimLayer.Opacity = Math.Clamp(theme.BackgroundDim, 0, 1);
    }

    // ── Caja: arrastre y tamaño ──────────────────────────────────

    private void SetBox(double x, double y, double w, double h)
    {
        Canvas.SetLeft(BoxBorder, Math.Clamp(x, 0, CanvasW - 50));
        Canvas.SetTop(BoxBorder, Math.Clamp(y, 0, CanvasH - 30));
        BoxBorder.Width = Math.Clamp(w, 50, CanvasW);
        BoxBorder.Height = Math.Clamp(h, 30, CanvasH);
        UpdateBoxInfo();
    }

    private void Box_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Thumb)
            return;
        _dragging = true;
        var position = e.GetPosition(DesignCanvas);
        _dragOffset = new Point(
            position.X - Canvas.GetLeft(BoxBorder),
            position.Y - Canvas.GetTop(BoxBorder));
        BoxBorder.CaptureMouse();
    }

    private void Box_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;

        var position = e.GetPosition(DesignCanvas);
        var x = Math.Clamp(position.X - _dragOffset.X, 0, CanvasW - BoxBorder.Width);
        var y = Math.Clamp(position.Y - _dragOffset.Y, 0, CanvasH - BoxBorder.Height);
        Canvas.SetLeft(BoxBorder, x);
        Canvas.SetTop(BoxBorder, y);
        UpdateBoxInfo();
    }

    private void Box_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        BoxBorder.ReleaseMouseCapture();
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var maxW = CanvasW - Canvas.GetLeft(BoxBorder);
        var maxH = CanvasH - Canvas.GetTop(BoxBorder);
        BoxBorder.Width = Math.Clamp(BoxBorder.Width + e.HorizontalChange, 50, maxW);
        BoxBorder.Height = Math.Clamp(BoxBorder.Height + e.VerticalChange, 30, maxH);
        UpdateBoxInfo();
    }

    private void UpdateBoxInfo()
    {
        BoxInfo.Text = $"X {Canvas.GetLeft(BoxBorder) / Scale:0} · Y {Canvas.GetTop(BoxBorder) / Scale:0} · " +
                       $"Ancho {BoxBorder.Width / Scale:0} · Alto {BoxBorder.Height / Scale:0}";
    }

    // ── Formato ──────────────────────────────────────────────────

    private void Style_Changed(object sender, RoutedEventArgs e) => UpdateBoxText();

    private void UpdateBoxText()
    {
        if (_loading)
            return;

        FontSizeValue.Text = $"{FontSizeSlider.Value:0}";
        BoxText.FontSize = Math.Max(8, FontSizeSlider.Value * Scale);
        BoxText.FontWeight = BoldCheck.IsChecked == true ? FontWeights.SemiBold : FontWeights.Normal;
        BoxText.FontStyle = ItalicCheck.IsChecked == true ? FontStyles.Italic : FontStyles.Normal;

        try
        {
            BoxText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(ColorBox.Text.Trim()));
        }
        catch
        {
            BoxText.Foreground = Brushes.White;
        }

        BoxText.TextAlignment = AlignHCombo.SelectedIndex switch
        {
            0 => TextAlignment.Left,
            2 => TextAlignment.Right,
            _ => TextAlignment.Center,
        };
        BoxText.VerticalAlignment = AlignVCombo.SelectedIndex switch
        {
            0 => VerticalAlignment.Top,
            2 => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center,
        };
    }

    // ── Acciones ─────────────────────────────────────────────────

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        Saved = true;
        Result = null;
        DialogResult = true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Solo se guarda lo que difiere del tema: lo demás sigue heredando.
        var fontSize = Math.Round(FontSizeSlider.Value);
        var bold = BoldCheck.IsChecked == true;
        var italic = ItalicCheck.IsChecked == true;
        var color = ColorBox.Text.Trim();
        var alignH = (HAlign)Math.Clamp(AlignHCombo.SelectedIndex, 0, 2);
        var alignV = (VAlign)Math.Clamp(AlignVCombo.SelectedIndex, 0, 2);

        var result = new SlideOverride(
            BoxX: Math.Round(Canvas.GetLeft(BoxBorder) / Scale),
            BoxY: Math.Round(Canvas.GetTop(BoxBorder) / Scale),
            BoxWidth: Math.Round(BoxBorder.Width / Scale),
            BoxHeight: Math.Round(BoxBorder.Height / Scale),
            FontSize: Math.Abs(fontSize - _theme.MaxFontSize) < 0.5 ? null : fontSize,
            Bold: bold == _theme.Bold ? null : bold,
            Italic: italic == _theme.Italic ? null : italic,
            TextColor: string.Equals(color, _theme.TextColor, StringComparison.OrdinalIgnoreCase) ? null : color,
            AlignH: alignH == _theme.AlignH ? null : alignH,
            AlignV: alignV == _theme.AlignV ? null : alignV);

        Saved = true;
        Result = result;
        DialogResult = true;
    }
}
