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
/// its theme and per-slide overrides, with the current output state. Used at
/// full size by the output window and scaled down by the previews.
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
    private const double CaptionBandFactor = 1.7;

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

    // ── Estilo efectivo: tema + overrides del slide ──────────────

    private double EffectiveMaxFontSize => Slide?.Override?.FontSize ?? CurrentTheme.MaxFontSize;
    private string EffectiveFontFamily => Slide?.Override?.FontFamily ?? CurrentTheme.FontFamily;
    private bool EffectiveBold => Slide?.Override?.Bold ?? CurrentTheme.Bold;
    private bool EffectiveItalic => Slide?.Override?.Italic ?? CurrentTheme.Italic;
    private bool EffectiveUnderline => Slide?.Override?.Underline ?? false;
    private bool EffectiveStrikethrough => Slide?.Override?.Strikethrough ?? false;
    private bool EffectiveShadow => Slide?.Override?.Shadow ?? CurrentTheme.Shadow;
    private string EffectiveTextColor => Slide?.Override?.TextColor ?? CurrentTheme.TextColor;
    private HAlign EffectiveAlignH => Slide?.Override?.AlignH ?? CurrentTheme.AlignH;
    private VAlign EffectiveAlignV => Slide?.Override?.AlignV ?? CurrentTheme.AlignV;
    private double? EffectiveLineSpacing => Slide?.Override?.LineSpacing;
    private bool EffectiveFitToWidth => Slide?.Override?.FitToWidth ?? CurrentTheme.FitToWidth;

    private TextCase EffectiveCase =>
        Slide?.Override?.Case ?? (CurrentTheme.Uppercase ? TextCase.Upper : TextCase.None);

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

        RootCanvas.Background = theme.TransparentBackground
            ? Brushes.Transparent
            : BrushFromHex(theme.BackgroundColor, "#10141E");
        ApplyBackgroundImage(theme.BackgroundImagePath);
        DimLayer.Opacity = Math.Clamp(theme.BackgroundDim, 0, 1);

        TextStack.HorizontalAlignment = EffectiveAlignH switch
        {
            HAlign.Left => HorizontalAlignment.Left,
            HAlign.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center,
        };
        TextStack.VerticalAlignment = EffectiveAlignV switch
        {
            VAlign.Top => VerticalAlignment.Top,
            VAlign.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center,
        };

        var fontFamily = new FontFamily(EffectiveFontFamily);
        var alignment = EffectiveAlignH switch
        {
            HAlign.Left => TextAlignment.Left,
            HAlign.Right => TextAlignment.Right,
            _ => TextAlignment.Center,
        };
        var foreground = BrushFromHex(EffectiveTextColor, "#FFFFFF");
        var effect = EffectiveShadow
            ? new DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.75 }
            : null;
        var decorations = BuildDecorations();

        MainText.FontFamily = fontFamily;
        MainText.FontWeight = EffectiveBold ? FontWeights.SemiBold : FontWeights.Normal;
        MainText.FontStyle = EffectiveItalic ? FontStyles.Italic : FontStyles.Normal;
        MainText.Foreground = foreground;
        MainText.TextAlignment = alignment;
        MainText.TextDecorations = decorations;
        MainText.Effect = effect;

        SecondaryText.FontFamily = fontFamily;
        SecondaryText.TextAlignment = alignment;
        SecondaryText.Effect = effect;
        SecondaryText.Margin = new Thickness(0, SecondarySpacing, 0, 0);

        CaptionLayer.Margin = new Thickness(theme.MarginHorizontal, theme.MarginVertical * 0.5,
            theme.MarginHorizontal, theme.MarginVertical * 0.5);
        CaptionText.FontFamily = new FontFamily(theme.FontFamily);
        CaptionText.FontSize = theme.CaptionFontSize;
        CaptionText.HorizontalAlignment = theme.CaptionPosition switch
        {
            CaptionPosition.TopLeft or CaptionPosition.BottomLeft => HorizontalAlignment.Left,
            CaptionPosition.TopCenter or CaptionPosition.BottomCenter => HorizontalAlignment.Center,
            _ => HorizontalAlignment.Right,
        };
        CaptionText.VerticalAlignment = IsCaptionOnTop(theme.CaptionPosition)
            ? VerticalAlignment.Top
            : VerticalAlignment.Bottom;
    }

    private static bool IsCaptionOnTop(CaptionPosition position) =>
        position is CaptionPosition.TopLeft or CaptionPosition.TopCenter or CaptionPosition.TopRight;

    private TextDecorationCollection? BuildDecorations()
    {
        if (!EffectiveUnderline && !EffectiveStrikethrough)
            return null;

        var decorations = new TextDecorationCollection();
        if (EffectiveUnderline)
            decorations.Add(TextDecorations.Underline);
        if (EffectiveStrikethrough)
            decorations.Add(TextDecorations.Strikethrough);
        return decorations;
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

    // ── Área de texto ────────────────────────────────────────────

    /// <summary>
    /// Where the main text lives: the slide's own box if it has one, or the
    /// theme margins minus a reserved band for the caption so they never
    /// overlap.
    /// </summary>
    private (Thickness Margin, double Width, double Height) ComputeTextArea(bool hasCaption)
    {
        var theme = CurrentTheme;
        var over = Slide?.Override;

        // Per-slide box wins; then the theme's default box; then margins.
        double? bx = over?.BoxX ?? theme.BoxX;
        double? by = over?.BoxY ?? theme.BoxY;
        double? bw = over?.BoxWidth ?? theme.BoxWidth;
        double? bh = over?.BoxHeight ?? theme.BoxHeight;

        if (bx is not null && by is not null && bw is not null && bh is not null)
        {
            var x = Math.Clamp(bx.Value, 0, CanvasWidth - 100);
            var y = Math.Clamp(by.Value, 0, CanvasHeight - 60);
            var w = Math.Clamp(bw.Value, 100, CanvasWidth - x);
            var h = Math.Clamp(bh.Value, 60, CanvasHeight - y);
            return (new Thickness(x, y, CanvasWidth - x - w, CanvasHeight - y - h), w, h);
        }

        var top = theme.MarginVertical;
        var bottom = theme.MarginVertical;
        if (hasCaption && theme.ShowCaption)
        {
            var band = theme.CaptionFontSize * CaptionBandFactor;
            if (IsCaptionOnTop(theme.CaptionPosition))
                top += band;
            else
                bottom += band;
        }

        var width = Math.Max(200, CanvasWidth - 2 * theme.MarginHorizontal);
        var height = Math.Max(150, CanvasHeight - top - bottom);
        return (new Thickness(theme.MarginHorizontal, top, theme.MarginHorizontal, bottom), width, height);
    }

    // ── Texto ────────────────────────────────────────────────────

    private void RenderText()
    {
        var theme = CurrentTheme;
        var main = Transform(Slide?.MainText);
        var secondary = Transform(Slide?.SecondaryText);

        var hasCaption = !string.IsNullOrEmpty(Slide?.Caption);
        var (margin, areaWidth, areaHeight) = ComputeTextArea(hasCaption);
        TextLayer.Margin = margin;

        // "Fit to width" keeps each written line unbroken; otherwise wrap.
        var wrapping = EffectiveFitToWidth ? TextWrapping.NoWrap : TextWrapping.Wrap;
        MainText.TextWrapping = wrapping;
        SecondaryText.TextWrapping = wrapping;

        var fontSize = FitFontSize(main, secondary, theme, areaWidth, areaHeight);
        MainText.FontSize = fontSize;
        SecondaryText.FontSize = Math.Max(20, fontSize * SecondaryRatio);

        if (EffectiveLineSpacing is double spacing && spacing > 0)
        {
            MainText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            MainText.LineHeight = fontSize * spacing;
        }
        else
        {
            MainText.LineHeight = double.NaN;
        }

        RenderWithHighlight(MainText, main);
        RenderWithHighlight(SecondaryText, secondary);
        SecondaryText.Visibility = string.IsNullOrEmpty(secondary)
            ? Visibility.Collapsed
            : Visibility.Visible;

        CaptionText.Text = Slide?.Caption ?? string.Empty;
        CaptionText.Visibility = theme.ShowCaption && hasCaption
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private string? Transform(string? text)
    {
        if (text is null)
            return null;

        var culture = CultureInfo.CurrentCulture;
        return EffectiveCase switch
        {
            TextCase.Upper => text.ToUpper(culture),
            TextCase.Title => culture.TextInfo.ToTitleCase(text.ToLower(culture)),
            TextCase.Sentence => ToSentenceCase(text, culture),
            _ => text,
        };
    }

    private static string ToSentenceCase(string text, CultureInfo culture)
    {
        var chars = text.ToCharArray();
        var startOfSentence = true;
        for (var i = 0; i < chars.Length; i++)
        {
            if (startOfSentence && char.IsLetter(chars[i]))
            {
                chars[i] = char.ToUpper(chars[i], culture);
                startOfSentence = false;
            }
            else if (chars[i] is '.' or '!' or '?' or '\n')
            {
                startOfSentence = true;
            }
        }
        return new string(chars);
    }

    /// <summary>
    /// Starts at the preferred size and shrinks until the whole text fits
    /// the available area (never below the theme's minimum).
    /// </summary>
    private double FitFontSize(string? main, string? secondary, SlideTheme theme, double width, double height)
    {
        var maxSize = EffectiveMaxFontSize;
        if (string.IsNullOrEmpty(main))
            return maxSize;

        // Fit-to-width can shrink well below the theme minimum, since its
        // whole point is to keep long lines unbroken.
        var minSize = EffectiveFitToWidth ? 8 : Math.Min(theme.MinFontSize, maxSize);

        for (var size = maxSize; size >= minSize; size -= 2)
        {
            var fits = EffectiveFitToWidth
                ? WidestLine(main, size, theme) <= width && MeasureHeight(main, size, theme, width) <= height
                : MeasureHeight(main, size, theme, width) <= height;

            if (fits && !string.IsNullOrEmpty(secondary))
            {
                var secondarySize = size * SecondaryRatio;
                fits = MeasureHeight(secondary, secondarySize, theme, width) + SecondarySpacing
                    <= height - MeasureHeight(main, size, theme, width);
                if (EffectiveFitToWidth)
                    fits = fits && WidestLine(secondary, secondarySize, theme) <= width;
            }

            if (fits)
                return size;
        }

        return minSize;
    }

    /// <summary>Width of the longest line (split on hard breaks) at a given size, unwrapped.</summary>
    private double WidestLine(string text, double fontSize, SlideTheme theme)
    {
        var widest = 0d;
        foreach (var line in text.Split('\n'))
        {
            var formatted = FormatLine(line.Length == 0 ? " " : line, fontSize, theme, double.PositiveInfinity);
            widest = Math.Max(widest, formatted.WidthIncludingTrailingWhitespace);
        }
        return widest;
    }

    private double MeasureHeight(string text, double fontSize, SlideTheme theme, double maxWidth) =>
        FormatLine(text, fontSize, theme, maxWidth).Height;

    private FormattedText FormatLine(string text, double fontSize, SlideTheme theme, double maxWidth)
    {
        var typeface = new Typeface(
            new FontFamily(EffectiveFontFamily),
            EffectiveItalic ? FontStyles.Italic : FontStyles.Normal,
            EffectiveBold ? FontWeights.SemiBold : FontWeights.Normal,
            FontStretches.Normal);

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        if (!double.IsPositiveInfinity(maxWidth))
            formatted.MaxTextWidth = maxWidth;

        return formatted;
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
        var contentVisible = State == OutputState.Content ? Visibility.Visible : Visibility.Hidden;
        TextLayer.Visibility = contentVisible;
        CaptionLayer.Visibility = contentVisible;
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
