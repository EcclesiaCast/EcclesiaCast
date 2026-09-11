using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
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

    /// <summary>The logo shown in the Logo state; null uses the built-in wordmark.</summary>
    public static readonly DependencyProperty LogoProperty =
        DependencyProperty.Register(nameof(Logo), typeof(Logo), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).RenderLogo()));

    /// <summary>
    /// True in the output window, where a video logo plays on its own surface
    /// behind this control — the logo layer then stays transparent so it
    /// shows through, exactly like a video background.
    /// </summary>
    public static readonly DependencyProperty IsLiveOutputProperty =
        DependencyProperty.Register(nameof(IsLiveOutput), typeof(bool), typeof(SlideView),
            new PropertyMetadata(false, (d, _) => ((SlideView)d).RenderLogo()));

    /// <summary>
    /// The "we start in 5:00" screen. Null while there is no countdown, which
    /// is most of the time.
    /// </summary>
    public static readonly DependencyProperty CountdownProperty =
        DependencyProperty.Register(nameof(Countdown), typeof(Countdown), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).OnCountdownChanged()));

    public Countdown? Countdown
    {
        get => (Countdown?)GetValue(CountdownProperty);
        set => SetValue(CountdownProperty, value);
    }

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
    private DispatcherTimer? _countdownTicker;

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

    public Logo? Logo
    {
        get => (Logo?)GetValue(LogoProperty);
        set => SetValue(LogoProperty, value);
    }

    public bool IsLiveOutput
    {
        get => (bool)GetValue(IsLiveOutputProperty);
        set => SetValue(IsLiveOutputProperty, value);
    }

    /// <summary>Re-reads the slide's theme and re-renders. Call after editing themes.</summary>
    public void Refresh() => OnSlideChanged();

    private bool _overMedia;

    /// <summary>
    /// True while a media background (image or video) is showing behind this
    /// view. The theme's own background colour then has to stay transparent or
    /// it paints straight over the media: that is what hid a background applied
    /// while no slide was live, since the fallback theme is opaque.
    /// </summary>
    public bool IsOverMedia
    {
        get => _overMedia;
        set
        {
            if (_overMedia == value)
                return;
            _overMedia = value;
            ApplyTheme();
        }
    }

    private SlideTheme CurrentTheme => Slide?.Theme ?? SlideTheme.Fallback;

    // ── Estilo efectivo: tema + overrides del slide ──────────────

    private double EffectiveMaxFontSize => Slide?.Override?.FontSize ?? CurrentTheme.MaxFontSize;
    private string EffectiveFontFamily => Slide?.Override?.FontFamily ?? CurrentTheme.FontFamily;
    private bool EffectiveBold => Slide?.Override?.Bold ?? CurrentTheme.Bold;
    private bool EffectiveItalic => Slide?.Override?.Italic ?? CurrentTheme.Italic;
    private bool EffectiveUnderline => Slide?.Override?.Underline ?? false;
    private bool EffectiveStrikethrough => Slide?.Override?.Strikethrough ?? false;
    private bool EffectiveShadow => Slide?.Override?.Shadow ?? CurrentTheme.Shadow;
    private double EffectiveShadowOpacity => Slide?.Override?.ShadowOpacity ?? CurrentTheme.ShadowOpacity;
    private double EffectiveOutlineWidth => Slide?.Override?.OutlineWidth ?? CurrentTheme.OutlineWidth;
    private string EffectiveOutlineColor => Slide?.Override?.OutlineColor ?? CurrentTheme.OutlineColor;
    private string EffectiveTextColor => Slide?.Override?.TextColor ?? CurrentTheme.TextColor;
    private HAlign EffectiveAlignH => Slide?.Override?.AlignH ?? CurrentTheme.AlignH;
    private VAlign EffectiveAlignV => Slide?.Override?.AlignV ?? CurrentTheme.AlignV;
    private double? EffectiveLineSpacing => Slide?.Override?.LineSpacing;
    private bool EffectiveFitToWidth => Slide?.Override?.FitToWidth ?? CurrentTheme.FitToWidth;

    private TextCase EffectiveCase =>
        Slide?.Override?.Case ?? CurrentTheme.TextCase;

    /// <summary>
    /// Draws the slide's extra text boxes. They are part of the slide's own
    /// design rather than the theme, so they are rebuilt whenever the slide
    /// changes; most slides have none and this does nothing.
    /// </summary>
    private void RenderExtraBoxes()
    {
        ExtraBoxLayer.Children.Clear();

        foreach (var box in Slide?.Override?.TextBoxes ?? [])
        {
            var text = new TextBlock
            {
                Text = Transform(box.Text) ?? string.Empty,
                FontFamily = new FontFamily(string.IsNullOrWhiteSpace(box.FontFamily)
                    ? EffectiveFontFamily
                    : box.FontFamily),
                FontSize = Math.Max(8, box.FontSize),
                FontWeight = box.Bold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = box.Italic ? FontStyles.Italic : FontStyles.Normal,
                Foreground = BrushFrom(box.Color, Brushes.White),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = box.AlignH switch
                {
                    HAlign.Left => TextAlignment.Left,
                    HAlign.Right => TextAlignment.Right,
                    _ => TextAlignment.Center,
                },
            };

            // The box is a frame the text sits inside, exactly like the main
            // one: the vertical alignment decides where within it.
            var holder = new Grid
            {
                Width = Math.Max(20, box.Width),
                Height = Math.Max(20, box.Height),
            };
            text.VerticalAlignment = box.AlignV switch
            {
                VAlign.Top => VerticalAlignment.Top,
                VAlign.Bottom => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center,
            };
            text.HorizontalAlignment = HorizontalAlignment.Stretch;
            holder.Children.Add(text);

            Canvas.SetLeft(holder, box.X);
            Canvas.SetTop(holder, box.Y);
            ExtraBoxLayer.Children.Add(holder);
        }
    }

    private static Brush BrushFrom(string? hex, Brush fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    private void OnSlideChanged()
    {
        ApplyTheme();
        RenderText();
        RenderExtraBoxes();

        if (AnimateTransitions && State == OutputState.Content)
            TransitionIn(TextLayer);
    }

    // ── Tema ─────────────────────────────────────────────────────

    private void ApplyTheme()
    {
        var theme = CurrentTheme;

        RootCanvas.Background = theme.TransparentBackground || _overMedia
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
        var shadow = BuildShadow();
        var outline = BuildOutline();
        var decorations = BuildDecorations();

        MainText.FontFamily = fontFamily;
        MainText.FontWeight = EffectiveBold ? FontWeights.SemiBold : FontWeights.Normal;
        MainText.FontStyle = EffectiveItalic ? FontStyles.Italic : FontStyles.Normal;
        MainText.Foreground = foreground;
        MainText.TextAlignment = alignment;
        MainText.TextDecorations = decorations;
        MainText.Effect = outline;
        MainTextShadow.Effect = shadow;

        // La 2ª versión bíblica: mismo estilo que la principal, o el suyo.
        var matches = theme.SecondaryMatchesPrimary;
        SecondaryText.FontFamily = fontFamily;
        SecondaryText.FontWeight = matches && EffectiveBold ? FontWeights.SemiBold : FontWeights.Normal;
        SecondaryText.FontStyle = (matches ? EffectiveItalic : theme.SecondaryItalic)
            ? FontStyles.Italic : FontStyles.Normal;
        SecondaryText.Foreground = matches ? foreground : BrushFromHex(theme.SecondaryColor, "#C9D4E8");
        SecondaryText.TextAlignment = alignment;
        SecondaryText.Effect = BuildOutline();
        SecondaryTextShadow.Effect = BuildShadow();
        SecondaryTextShadow.Margin = new Thickness(0, SecondarySpacing, 0, 0);

        CaptionLayer.Margin = new Thickness(theme.MarginHorizontal, theme.MarginVertical * 0.5,
            theme.MarginHorizontal, theme.MarginVertical * 0.5);
        CaptionText.FontFamily = new FontFamily(theme.CaptionFontFamily ?? theme.FontFamily);
        CaptionText.Foreground = BrushFromHex(theme.CaptionColor, "#B9C6DE");
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

    /// <summary>The drop shadow behind the text, at the configured strength.</summary>
    private DropShadowEffect? BuildShadow()
    {
        var opacity = Math.Clamp(EffectiveShadowOpacity, 0, 1);
        if (!EffectiveShadow || opacity <= 0)
            return null;

        return new DropShadowEffect
        {
            BlurRadius = Math.Clamp(CurrentTheme.ShadowBlur, 0, 80),
            ShadowDepth = 3,
            Opacity = opacity,
        };
    }

    /// <summary>
    /// The outline: a tight, fully opaque halo of the outline colour hugging
    /// every letter. Rendered as a zero-depth shadow because WPF text has no
    /// stroke of its own — and unlike stroking the glyphs, this keeps the
    /// highlighted runs intact.
    /// </summary>
    private DropShadowEffect? BuildOutline()
    {
        var width = Math.Clamp(EffectiveOutlineWidth, 0, 40);
        if (width <= 0)
            return null;

        return new DropShadowEffect
        {
            Color = ColorFromHex(EffectiveOutlineColor, "#000000"),
            BlurRadius = width,
            ShadowDepth = 0,
            Opacity = 1,
            RenderingBias = RenderingBias.Quality,
        };
    }

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
                // Remote posters (YouTube) are still downloading and can't be frozen.
                if (bitmap.CanFreeze)
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

    private static Brush BrushFromHex(string hex, string fallback) =>
        new SolidColorBrush(ColorFromHex(hex, fallback));

    private static Color ColorFromHex(string? hex, string fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex ?? fallback);
        }
        catch
        {
            return (Color)ColorConverter.ConvertFromString(fallback);
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
        var secondaryScale = theme.SecondaryMatchesPrimary ? 1.0 : theme.SecondaryScale;
        SecondaryText.FontSize = Math.Max(20, fontSize * secondaryScale);

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
            TextCase.Lower => text.ToLower(culture),
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
                var secondarySize = size * (theme.SecondaryMatchesPrimary ? 1.0 : theme.SecondaryScale);
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

    // ── Logo ─────────────────────────────────────────────────────

    private string? _lastLogoImagePath;

    /// <summary>
    /// Paints the Logo state: a colour, plus an image (or the frame of a video
    /// logo in the previews) or free text. In the live output a video logo
    /// leaves this layer transparent, since the moving picture plays behind.
    /// </summary>
    private void RenderLogo()
    {
        var logo = Logo;

        if (logo is null)
        {
            LogoLayer.Background = BrushFromHex("#10141E", "#10141E");
            LogoImage.Visibility = Visibility.Collapsed;
            LogoImage.Source = null;
            _lastLogoImagePath = null;
            LogoText.Text = "EcclesiaCast";
            LogoText.FontFamily = new FontFamily("Segoe UI");
            LogoText.FontSize = 120;
            LogoText.FontWeight = FontWeights.SemiBold;
            LogoText.Foreground = Brushes.White;
            LogoText.Visibility = Visibility.Visible;
            return;
        }

        var isLiveVideo = logo.Kind == LogoKind.Video && IsLiveOutput;

        // The video surface behind paints the whole screen, so anything drawn
        // here (even the background colour) would cover it.
        LogoLayer.Background = isLiveVideo
            ? Brushes.Transparent
            : BrushFromHex(logo.BackgroundColor, "#10141E");

        LogoText.Visibility = logo.Kind == LogoKind.Text ? Visibility.Visible : Visibility.Collapsed;
        if (logo.Kind == LogoKind.Text)
        {
            LogoText.Text = logo.Text;
            LogoText.FontFamily = new FontFamily(
                string.IsNullOrWhiteSpace(logo.FontFamily) ? "Segoe UI" : logo.FontFamily);
            LogoText.FontSize = Math.Clamp(logo.FontSize, 10, 400);
            LogoText.FontWeight = logo.Bold ? FontWeights.SemiBold : FontWeights.Normal;
            LogoText.Foreground = BrushFromHex(logo.TextColor, "#FFFFFF");
        }

        LogoImage.Stretch = logo.Scaling switch
        {
            MediaScaling.Fill => Stretch.UniformToFill,
            MediaScaling.Stretch => Stretch.Fill,
            _ => Stretch.Uniform,
        };

        // An image logo shows its file; a video logo shows its poster, but only
        // in the previews (live, the real video is playing behind this layer).
        var path = logo.Kind switch
        {
            LogoKind.Image => logo.Path,
            LogoKind.Video when !IsLiveOutput => logo.PosterPath,
            _ => null,
        };

        if (path == _lastLogoImagePath)
            return;
        _lastLogoImagePath = path;

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
                if (bitmap.CanFreeze)
                    bitmap.Freeze();
                LogoImage.Source = bitmap;
                LogoImage.Visibility = Visibility.Visible;
                return;
            }
            catch
            {
                // Ilegible: queda el color de fondo del logo.
            }
        }

        LogoImage.Source = null;
        LogoImage.Visibility = Visibility.Collapsed;
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
                TransitionIn(TextLayer);
        }
        else
        {
            BlackLayer.Opacity = blackTarget;
        }
    }

    /// <summary>
    /// The countdown redraws once a second while it is up, and the timer is
    /// only alive during that time — every preview box on the operator's
    /// screen is one of these controls, and none of them should be waking up
    /// every second for a screen nobody asked for.
    /// </summary>
    private void OnCountdownChanged()
    {
        _countdownTicker ??= new DispatcherTimer(
            TimeSpan.FromMilliseconds(250), DispatcherPriority.Normal, (_, _) => RenderCountdown(), Dispatcher);

        if (Countdown is null)
        {
            _countdownTicker.Stop();
            CountdownLayer.Visibility = Visibility.Collapsed;
            return;
        }

        // Quarter-second ticks rather than one-second ones: on a full second
        // the displayed number and the clock drift apart just enough to make
        // the last seconds stutter.
        _countdownTicker.Start();
        RenderCountdown();

        if (AnimateTransitions)
            FadeIn(CountdownLayer);
    }

    private void RenderCountdown()
    {
        if (Countdown is not { } countdown)
            return;

        CountdownLayer.Visibility = Visibility.Visible;

        var finished = countdown.HasFinished(DateTimeOffset.Now);

        // Once it hits zero the heading has nothing left to introduce, and
        // leaving it up reads as one sentence: "Empezamos en ¡Bienvenidos!".
        CountdownHeading.Text = countdown.Heading ?? string.Empty;
        CountdownHeading.Visibility = finished || string.IsNullOrWhiteSpace(countdown.Heading)
            ? Visibility.Collapsed
            : Visibility.Visible;

        var text = countdown.Format(DateTimeOffset.Now);
        if (CountdownClock.Text != text)
            CountdownClock.Text = text;

        // A closing message ("¡Bienvenidos!") is words, not digits: at the
        // clock's size it would run off both sides of the screen.
        CountdownClock.FontSize = finished ? 150 : 260;
        CountdownClock.TextWrapping = finished ? TextWrapping.Wrap : TextWrapping.NoWrap;
        CountdownClock.MaxWidth = finished ? CanvasWidth - 220 : double.PositiveInfinity;
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

    // ── Transición entre diapositivas ────────────────────────────

    private SlideTransition EffectiveTransition => CurrentTheme.Transition;

    /// <summary>Clamped: a transition longer than a second outlasts the change it covers.</summary>
    private double EffectiveTransitionMs => Math.Clamp(CurrentTheme.TransitionMs, 0, 1500);

    /// <summary>
    /// Brings the words in the way the theme asks. Each theme carries its own,
    /// so a church can have the songs fade and the Bible cut — reading a verse
    /// is a different act from following a lyric.
    /// </summary>
    private void TransitionIn(FrameworkElement element)
    {
        var duration = TimeSpan.FromMilliseconds(EffectiveTransitionMs);
        var transition = EffectiveTransition;

        // Whatever an earlier transition left behind has to go, or a theme
        // switched mid-service would keep sliding a slide that should cut.
        element.BeginAnimation(OpacityProperty, null);
        element.RenderTransform = System.Windows.Media.Transform.Identity;
        element.Opacity = 1;

        if (transition == SlideTransition.None || EffectiveTransitionMs <= 0)
            return;

        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        element.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, duration) { EasingFunction = ease });

        if (transition == SlideTransition.Fade)
            return;

        element.RenderTransformOrigin = new Point(0.5, 0.5);

        switch (transition)
        {
            case SlideTransition.SlideLeft:
            {
                var move = new TranslateTransform();
                element.RenderTransform = move;
                move.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(CanvasWidth * 0.06, 0, duration) { EasingFunction = ease });
                break;
            }

            case SlideTransition.SlideUp:
            {
                var move = new TranslateTransform();
                element.RenderTransform = move;
                move.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(CanvasHeight * 0.06, 0, duration) { EasingFunction = ease });
                break;
            }

            case SlideTransition.Zoom:
            {
                var scale = new ScaleTransform(1.06, 1.06);
                element.RenderTransform = scale;
                var settle = new DoubleAnimation(1.06, 1, duration) { EasingFunction = ease };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, settle);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, settle);
                break;
            }
        }
    }
}
