using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
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
            new PropertyMetadata(OutputState.Content, (d, e) => ((SlideView)d).OnStateChanged((OutputState)e.OldValue)));

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

    /// <summary>The blue of a text selection, for the words being dragged over on the Live preview.</summary>
    private static readonly Brush SelectionBrush = new SolidColorBrush(Color.FromArgb(0xB0, 0x2F, 0x63, 0xC9));

    private (int Start, int End)? _selection;

    private const double CanvasWidth = SlideTextFit.CanvasWidth;
    private const double CanvasHeight = SlideTextFit.CanvasHeight;
    private const double SecondarySpacing = SlideTextFit.SecondarySpacing;

    private string? _lastBackgroundImagePath;
    private DispatcherTimer? _countdownTicker;

    public SlideView()
    {
        InitializeComponent();
        OnStateChanged(State);
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
        _selection = null;

        // The words on screen leave the way the new ones arrive; a copy of
        // them is taken before the new slide overwrites the text.
        var leaving = AnimateTransitions && State == OutputState.Content ? CopyOfText() : null;

        ApplyTheme();
        RenderText();
        RenderExtraBoxes();

        if (AnimateTransitions && State == OutputState.Content)
        {
            TransitionIn(TextLayer);
            if (leaving is not null)
                TransitionOut(leaving);
        }
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
        CaptionText.VerticalAlignment = SlideTextFit.IsCaptionOnTop(theme.CaptionPosition)
            ? VerticalAlignment.Top
            : VerticalAlignment.Bottom;
    }

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

    // ── Texto ────────────────────────────────────────────────────

    private void RenderText()
    {
        var theme = CurrentTheme;
        var main = Transform(Slide?.MainText);
        var secondary = Transform(Slide?.SecondaryText);

        // "Fit to width" keeps each written line unbroken; otherwise wrap.
        var wrapping = EffectiveFitToWidth ? TextWrapping.NoWrap : TextWrapping.Wrap;
        MainText.TextWrapping = wrapping;
        SecondaryText.TextWrapping = wrapping;

        var fit = SlideTextFit.Fit(Slide, main, secondary, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        TextLayer.Margin = fit.Margin;
        MainText.FontSize = fit.FontSize;
        var secondaryScale = theme.SecondaryMatchesPrimary ? 1.0 : theme.SecondaryScale;
        SecondaryText.FontSize = Math.Max(20, fit.FontSize * secondaryScale);

        if (EffectiveLineSpacing is double spacing && spacing > 0)
        {
            MainText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            MainText.LineHeight = fit.FontSize * spacing;
        }
        else
        {
            MainText.LineHeight = double.NaN;
        }

        RenderWithHighlight(MainText, main);
        RenderWithHighlight(SecondaryText, secondary);

        // The holder goes too, not just the text: its top margin left a gap
        // under every single-version verse that pushed it off centre and,
        // on a long one, out of its box.
        var hasSecondary = !string.IsNullOrEmpty(secondary);
        SecondaryText.Visibility = hasSecondary ? Visibility.Visible : Visibility.Collapsed;
        SecondaryTextShadow.Visibility = SecondaryText.Visibility;

        var hasCaption = !string.IsNullOrEmpty(Slide?.Caption);
        CaptionText.Text = Slide?.Caption ?? string.Empty;
        CaptionText.Visibility = theme.ShowCaption && hasCaption
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private string? Transform(string? text) => SlideTextFit.ApplyCase(text, EffectiveCase);

    /// <summary>
    /// The words of the main text between two points (in this control's
    /// coordinates), widened to whole words: a click gives the word under it,
    /// a drag the phrase it swept. Null when neither point is on the text.
    /// Used to highlight by pointing at the Live preview.
    /// </summary>
    public string? PhraseBetween(Point from, Point to)
    {
        if (SpanBetween(from, to) is not var (start, end))
            return null;

        var phrase = MainTextShown[start..end].Trim();
        return phrase.Length == 0 || !phrase.Any(char.IsLetterOrDigit) ? null : phrase;
    }

    /// <summary>
    /// Paints the words between two points as selected, the way a text box
    /// shows a selection while the mouse drags. Only this view shows it: the
    /// Live preview uses it so the operator sees what will be highlighted
    /// before letting go, and the screen only changes on release.
    /// </summary>
    public void ShowSelection(Point from, Point to)
    {
        var span = SpanBetween(from, to);
        if (span == _selection)
            return;

        _selection = span;
        RenderWithHighlight(MainText, Transform(Slide?.MainText));
    }

    public void ClearSelection()
    {
        if (_selection is null)
            return;

        _selection = null;
        RenderWithHighlight(MainText, Transform(Slide?.MainText));
    }

    /// <summary>The main text as it is on screen right now (casing applied).</summary>
    private string MainTextShown => string.Concat(MainText.Inlines.OfType<Run>().Select(r => r.Text));

    /// <summary>
    /// Where in the main text the words between two points start and end,
    /// widened to whole words. Null when neither point is on the text.
    /// </summary>
    private (int Start, int End)? SpanBetween(Point from, Point to)
    {
        var text = MainTextShown;
        if (text.Length == 0)
            return null;

        // Painting a selection rebuilds the text, and finding the letter
        // under the mouse refuses to work until it has been laid out again.
        MainText.UpdateLayout();

        var a = OffsetAt(from);
        var b = OffsetAt(to);
        if (a is null && b is null)
            return null;

        var start = Math.Min(a ?? b!.Value, b ?? a!.Value);
        var end = Math.Max(a ?? b!.Value, b ?? a!.Value);
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);

        // A click lands between two letters, or just after the last one.
        if (start == end && start > 0 && (start == text.Length || !IsWordChar(text[start])))
            start = end = start - 1;

        while (start > 0 && IsWordChar(text[start - 1]))
            start--;
        while (end < text.Length && IsWordChar(text[end]))
            end++;

        return start < end ? (start, end) : null;

        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '\'' or '’' or '-';
    }

    /// <summary>Character offset in the main text under a point, or null off the text.</summary>
    private int? OffsetAt(Point point)
    {
        var local = TranslatePoint(point, MainText);
        if (local.X < 0 || local.Y < 0 || local.X > MainText.ActualWidth || local.Y > MainText.ActualHeight)
            return null;

        var pointer = MainText.GetPositionFromPoint(local, snapToText: true);
        if (pointer?.Parent is not Run run)
            return null;

        var offset = 0;
        foreach (var inline in MainText.Inlines)
        {
            if (ReferenceEquals(inline, run))
                return offset + run.ContentStart.GetOffsetToPosition(pointer);
            if (inline is Run other)
                offset += other.Text.Length;
        }
        return null;
    }

    private void RenderWithHighlight(TextBlock target, string? text)
    {
        target.Inlines.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        // A selection being dragged on the Live preview wins over the
        // highlight: it shows exactly the words under the mouse.
        if (ReferenceEquals(target, MainText) && _selection is var (from, to) && to <= text.Length)
        {
            if (from > 0)
                target.Inlines.Add(new Run(text[..from]));
            target.Inlines.Add(new Run(text[from..to]) { Background = SelectionBrush });
            if (to < text.Length)
                target.Inlines.Add(new Run(text[to..]));
            return;
        }

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

    private void OnStateChanged(OutputState previous)
    {
        // Clearing the words (or covering them with the logo) takes them off
        // the way they would leave for the next slide. Black has its own fade.
        var leaving = AnimateTransitions
            && previous == OutputState.Content
            && State is OutputState.Clear or OutputState.Logo
                ? CopyOfText()
                : null;

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
            if (leaving is not null)
                TransitionOut(leaving);
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

    /// <summary>
    /// A still copy of the words on screen, to take them off with the same
    /// movement the next ones arrive with. Null when there is nothing to see
    /// leave, or when the theme cuts.
    /// </summary>
    private FrameworkElement? CopyOfText()
    {
        if (TextLayer.Visibility != Visibility.Visible
            || MainText.Inlines.Count == 0
            || EffectiveTransition == SlideTransition.None
            || EffectiveTransitionMs <= 0)
            return null;

        try
        {
            var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(TextLayer));
            copy.IsHitTestVisible = false;

            // Caught halfway through its own entrance, it leaves from there.
            copy.Opacity = TextLayer.Opacity;
            copy.RenderTransform = System.Windows.Media.Transform.Identity;
            return copy;
        }
        catch (Exception)
        {
            // Without the copy the old words simply vanish, as they used to.
            return null;
        }
    }

    /// <summary>
    /// Takes the copy of the old words off the screen: it fades and moves the
    /// opposite way to the arrival (out to the left, up, or shrinking a
    /// little), then goes away.
    /// </summary>
    private void TransitionOut(FrameworkElement copy)
    {
        var duration = TimeSpan.FromMilliseconds(EffectiveTransitionMs);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseIn };

        RootCanvas.Children.Insert(RootCanvas.Children.IndexOf(TextLayer) + 1, copy);

        var fade = new DoubleAnimation(copy.Opacity, 0, duration) { EasingFunction = ease };
        fade.Completed += (_, _) => RootCanvas.Children.Remove(copy);

        copy.RenderTransformOrigin = new Point(0.5, 0.5);
        switch (EffectiveTransition)
        {
            case SlideTransition.SlideLeft:
            {
                var move = new TranslateTransform();
                copy.RenderTransform = move;
                move.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(0, -CanvasWidth * 0.06, duration) { EasingFunction = ease });
                break;
            }

            case SlideTransition.SlideUp:
            {
                var move = new TranslateTransform();
                copy.RenderTransform = move;
                move.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(0, -CanvasHeight * 0.06, duration) { EasingFunction = ease });
                break;
            }

            case SlideTransition.Zoom:
            {
                var scale = new ScaleTransform(1, 1);
                copy.RenderTransform = scale;
                var shrink = new DoubleAnimation(1, 0.94, duration) { EasingFunction = ease };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
                break;
            }
        }

        copy.BeginAnimation(OpacityProperty, fade);
    }
}
