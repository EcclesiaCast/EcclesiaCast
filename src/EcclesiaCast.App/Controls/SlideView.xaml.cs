using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Renders a slide (text + optional caption and secondary version) styled by
/// its theme, with the current output state. Used at full size by the output
/// window and scaled down by the previews.
/// </summary>
public partial class SlideView : UserControl
{
    public static readonly DependencyProperty SlideProperty =
        DependencyProperty.Register(nameof(Slide), typeof(SlideContent), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).OnSlideChanged()));

    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(nameof(State), typeof(OutputState), typeof(SlideView),
            new PropertyMetadata(OutputState.Content, (d, _) => ((SlideView)d).OnStateChanged()));

    public static readonly DependencyProperty AnimateTransitionsProperty =
        DependencyProperty.Register(nameof(AnimateTransitions), typeof(bool), typeof(SlideView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty OverlayProperty =
        DependencyProperty.Register(nameof(Overlay), typeof(string), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).OnOverlayChanged()));

    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(nameof(Highlight), typeof(string), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).RenderText()));

    private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xC3, 0x4A));

    private const double CanvasWidth = 1920;
    private const double CanvasHeight = 1080;
    private const double SecondaryRatio = 0.62;
    private const double SecondarySpacing = 50;

    private string? _lastBackgroundImagePath;

    public SlideView()
    {
        InitializeComponent();
        OnStateChanged();
    }

    public SlideContent? Slide
    {
        get => (SlideContent?)GetValue(SlideProperty);
        set => SetValue(SlideProperty, value);
    }

    public OutputState State
    {
        get => (OutputState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool AnimateTransitions
    {
        get => (bool)GetValue(AnimateTransitionsProperty);
        set => SetValue(AnimateTransitionsProperty, value);
    }

    public string? Overlay
    {
        get => (string?)GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    /// <summary>Word or phrase to paint over the projected text, marker-pen style.</summary>
    public string? Highlight
    {
        get => (string?)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <summary>Re-reads the slide's theme and re-renders. Call after editing themes.</summary>
    public void Refresh() => OnSlideChanged();

    private SlideTheme CurrentTheme => Slide?.Theme ?? SlideTheme.Fallback;

    private void OnSlideChanged()
    {
        ApplyTheme();
        RenderText();

        if (AnimateTransitions && State == OutputState.Content)
            FadeIn(TextLayer);
    }

    // ── Tema ─────────────────────────────────────────────────────

    private void ApplyTheme()
    {
        var theme = CurrentTheme;

        RootCanvas.Background = BrushFromHex(theme.BackgroundColor, "#10141E");
        ApplyBackgroundImage(theme.BackgroundImagePath);
        DimLayer.Opacity = Math.Clamp(theme.BackgroundDim, 0, 1);

        TextLayer.Margin = new Thickness(theme.MarginHorizontal, theme.MarginVertical,
            theme.MarginHorizontal, theme.MarginVertical);

        TextStack.HorizontalAlignment = theme.AlignH switch
        {
            HAlign.Left => HorizontalAlignment.Left,
            HAlign.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center,
        };
        TextStack.VerticalAlignment = theme.AlignV switch
        {
            VAlign.Top => VerticalAlignment.Top,
            VAlign.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center,
        };

        var fontFamily = new FontFamily(theme.FontFamily);
        var alignment = theme.AlignH switch
        {
            HAlign.Left => TextAlignment.Left,
            HAlign.Right => TextAlignment.Right,
            _ => TextAlignment.Center,
        };
        var foreground = BrushFromHex(theme.TextColor, "#FFFFFF");
        var effect = theme.Shadow
            ? new DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.75 }
            : null;

        MainText.FontFamily = fontFamily;
        MainText.FontWeight = theme.Bold ? FontWeights.SemiBold : FontWeights.Normal;
        MainText.FontStyle = theme.Italic ? FontStyles.Italic : FontStyles.Normal;
        MainText.Foreground = foreground;
        MainText.TextAlignment = alignment;
        MainText.Effect = effect;

        SecondaryText.FontFamily = fontFamily;
        SecondaryText.TextAlignment = alignment;
        SecondaryText.Effect = effect;
        SecondaryText.Margin = new Thickness(0, SecondarySpacing, 0, 0);

        CaptionText.FontFamily = fontFamily;
        CaptionText.FontSize = theme.CaptionFontSize;
        CaptionText.HorizontalAlignment = theme.CaptionPosition switch
        {
            CaptionPosition.TopLeft or CaptionPosition.BottomLeft => HorizontalAlignment.Left,
            CaptionPosition.TopCenter or CaptionPosition.BottomCenter => HorizontalAlignment.Center,
            _ => HorizontalAlignment.Right,
        };
        CaptionText.VerticalAlignment = theme.CaptionPosition switch
        {
            CaptionPosition.TopLeft or CaptionPosition.TopCenter or CaptionPosition.TopRight
                => VerticalAlignment.Top,
            _ => VerticalAlignment.Bottom,
        };
    }

    private void ApplyBackgroundImage(string? path)
    {
        if (path == _lastBackgroundImagePath)
            return;
        _lastBackgroundImagePath = path;

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 1920;
                bitmap.EndInit();
                bitmap.Freeze();
                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
                return;
            }
            catch
            {
                // Archivo ilegible: se cae al color de fondo.
            }
        }

        BackgroundImage.Source = null;
        BackgroundImage.Visibility = Visibility.Collapsed;
    }

    private static Brush BrushFromHex(string hex, string fallback)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));
        }
    }

    // ── Texto ────────────────────────────────────────────────────

    private void RenderText()
    {
        var theme = CurrentTheme;
        var main = Transform(Slide?.MainText, theme);
        var secondary = Transform(Slide?.SecondaryText, theme);

        var fontSize = FitFontSize(main, secondary, theme);
        MainText.FontSize = fontSize;
        SecondaryText.FontSize = Math.Max(20, fontSize * SecondaryRatio);

        RenderWithHighlight(MainText, main);
        RenderWithHighlight(SecondaryText, secondary);
        SecondaryText.Visibility = string.IsNullOrEmpty(secondary)
            ? Visibility.Collapsed
            : Visibility.Visible;

        CaptionText.Text = Slide?.Caption ?? string.Empty;
        CaptionText.Visibility = theme.ShowCaption && !string.IsNullOrEmpty(Slide?.Caption)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static string? Transform(string? text, SlideTheme theme) =>
        text is null ? null : theme.Uppercase ? text.ToUpper(CultureInfo.CurrentCulture) : text;

    /// <summary>
    /// Starts at the theme's preferred size and shrinks until the whole text
    /// fits inside the margins (never below the theme's minimum).
    /// </summary>
    private double FitFontSize(string? main, string? secondary, SlideTheme theme)
    {
        if (string.IsNullOrEmpty(main))
            return theme.MaxFontSize;

        var availableWidth = Math.Max(200, CanvasWidth - 2 * theme.MarginHorizontal);
        var availableHeight = Math.Max(150, CanvasHeight - 2 * theme.MarginVertical);

        for (var size = theme.MaxFontSize; size >= theme.MinFontSize; size -= 3)
        {
            var height = MeasureHeight(main, size, theme, availableWidth);
            if (!string.IsNullOrEmpty(secondary))
                height += SecondarySpacing + MeasureHeight(secondary, size * SecondaryRatio, theme, availableWidth);

            if (height <= availableHeight)
                return size;
        }

        return theme.MinFontSize;
    }

    private double MeasureHeight(string text, double fontSize, SlideTheme theme, double maxWidth)
    {
        var typeface = new Typeface(
            new FontFamily(theme.FontFamily),
            theme.Italic ? FontStyles.Italic : FontStyles.Normal,
            theme.Bold ? FontWeights.SemiBold : FontWeights.Normal,
            FontStretches.Normal);

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            MaxTextWidth = maxWidth,
        };

        return formatted.Height;
    }

    private void RenderWithHighlight(TextBlock target, string? text)
    {
        target.Inlines.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        var term = Highlight;
        if (string.IsNullOrWhiteSpace(term))
        {
            target.Inlines.Add(new Run(text));
            return;
        }

        var position = 0;
        while (position < text.Length)
        {
            var index = text.IndexOf(term, position, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
            {
                target.Inlines.Add(new Run(text[position..]));
                break;
            }

            if (index > position)
                target.Inlines.Add(new Run(text[position..index]));

            target.Inlines.Add(new Run(text.Substring(index, term.Length))
            {
                Background = HighlightBrush,
                Foreground = Brushes.Black,
            });

            position = index + term.Length;
        }
    }

    // ── Estados ──────────────────────────────────────────────────

    private void OnStateChanged()
    {
        TextLayer.Visibility = State == OutputState.Content ? Visibility.Visible : Visibility.Hidden;
        LogoLayer.Visibility = State == OutputState.Logo ? Visibility.Visible : Visibility.Collapsed;

        var blackTarget = State == OutputState.Black ? 1d : 0d;
        if (AnimateTransitions)
        {
            BlackLayer.BeginAnimation(OpacityProperty,
                new DoubleAnimation(blackTarget, TimeSpan.FromMilliseconds(250)));
            if (State == OutputState.Content)
                FadeIn(TextLayer);
        }
        else
        {
            BlackLayer.Opacity = blackTarget;
        }
    }

    private void OnOverlayChanged()
    {
        var hasMessage = !string.IsNullOrWhiteSpace(Overlay);
        OverlayText.Text = Overlay ?? string.Empty;
        OverlayLayer.Visibility = hasMessage ? Visibility.Visible : Visibility.Collapsed;

        if (AnimateTransitions && hasMessage)
            FadeIn(OverlayLayer);
    }

    private static void FadeIn(UIElement element) =>
        element.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new QuadraticEase()
            });
}
