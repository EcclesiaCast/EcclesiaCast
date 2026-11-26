using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Rendering;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// The slide on the projector, in Avalonia: the same control as the WPF
/// <c>SlideView</c> (text and its second version, caption, extra boxes, logo,
/// countdown, announcement, black), with the same properties. The size of the
/// letters comes from the shared <see cref="SlideTextFit"/>, measured with
/// Avalonia's own engine (<see cref="AvaloniaTextMetrics"/>).
///
/// Left out on purpose, for phase 3 (they belong to the operator): the
/// selection drawn while dragging over the Live preview (<c>ShowSelection</c>,
/// <c>PhraseBetween</c>). The logo is framed and filled like a background
/// (<see cref="MediaFraming"/>); a video logo's moving picture comes from the
/// output's hidden player (<see cref="ProjectedView"/>).
/// </summary>
public partial class SlideView : UserControl
{
    public static readonly StyledProperty<SlideContent?> SlideProperty =
        AvaloniaProperty.Register<SlideView, SlideContent?>(nameof(Slide));

    public static readonly StyledProperty<OutputState> StateProperty =
        AvaloniaProperty.Register<SlideView, OutputState>(nameof(State), OutputState.Content);

    public static readonly StyledProperty<bool> AnimateTransitionsProperty =
        AvaloniaProperty.Register<SlideView, bool>(nameof(AnimateTransitions));

    public static readonly StyledProperty<string?> OverlayProperty =
        AvaloniaProperty.Register<SlideView, string?>(nameof(Overlay));

    /// <summary>Typeface of the lower-third announcement; null or empty is Segoe UI.</summary>
    public static readonly StyledProperty<string?> OverlayFontFamilyProperty =
        AvaloniaProperty.Register<SlideView, string?>(nameof(OverlayFontFamily));

    /// <summary>Size of the lower-third announcement, in canvas units; held between 20 and 160.</summary>
    public static readonly StyledProperty<double> OverlayFontSizeProperty =
        AvaloniaProperty.Register<SlideView, double>(nameof(OverlayFontSize), OverlayStyle.DefaultSize);

    /// <summary>The logo shown in the Logo state; null uses the built-in wordmark.</summary>
    public static readonly StyledProperty<Logo?> LogoProperty =
        AvaloniaProperty.Register<SlideView, Logo?>(nameof(Logo));

    /// <summary>True in the output window (as in WPF; the video logo uses it from batch 10).</summary>
    public static readonly StyledProperty<bool> IsLiveOutputProperty =
        AvaloniaProperty.Register<SlideView, bool>(nameof(IsLiveOutput));

    /// <summary>On the real output, the picture a video logo is being played into.</summary>
    public static readonly StyledProperty<IImage?> LogoVideoFrameProperty =
        AvaloniaProperty.Register<SlideView, IImage?>(nameof(LogoVideoFrame));

    /// <summary>The "we start in 5:00" screen. Null while there is no countdown.</summary>
    public static readonly StyledProperty<Countdown?> CountdownProperty =
        AvaloniaProperty.Register<SlideView, Countdown?>(nameof(Countdown));

    public static readonly StyledProperty<HighlightSet?> HighlightProperty =
        AvaloniaProperty.Register<SlideView, HighlightSet?>(nameof(Highlight));

    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xC3, 0x4A));

    private const double CanvasWidth = SlideTextFit.CanvasWidth;
    private const double CanvasHeight = SlideTextFit.CanvasHeight;
    private const double SecondarySpacing = SlideTextFit.SecondarySpacing;

    /// <summary>How long the announcement and the countdown take to fade, as in WPF.</summary>
    private static readonly TimeSpan MessageFade = TimeSpan.FromMilliseconds(280);

    private static AvaloniaFontCatalog Fonts => AvaloniaFontCatalog.Current;

    private string? _lastBackgroundImagePath;
    private DispatcherTimer? _countdownTicker;
    private bool _lowDetail;
    private bool _overMedia;

    public SlideView()
    {
        InitializeComponent();

        // The XAML of WPF says «Segoe UI» on every text; here the font really
        // drawn for it (Selawik where it is missing).
        var segoe = Fonts.Resolve("Segoe UI");
        foreach (var text in new[] { MainText, SecondaryText, CaptionText, LogoText, CountdownHeading, CountdownClock, OverlayText })
            text.FontFamily = segoe;

        MainText.SizeChanged += (_, _) => UpdateBands();
        SecondaryText.SizeChanged += (_, _) => UpdateBands();
        TextLayer.SizeChanged += (_, _) => UpdateBands();

        OnStateChanged(State);
    }

    public SlideContent? Slide
    {
        get => GetValue(SlideProperty);
        set => SetValue(SlideProperty, value);
    }

    public OutputState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool AnimateTransitions
    {
        get => GetValue(AnimateTransitionsProperty);
        set => SetValue(AnimateTransitionsProperty, value);
    }

    public string? Overlay
    {
        get => GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    public string? OverlayFontFamily
    {
        get => GetValue(OverlayFontFamilyProperty);
        set => SetValue(OverlayFontFamilyProperty, value);
    }

    public double OverlayFontSize
    {
        get => GetValue(OverlayFontSizeProperty);
        set => SetValue(OverlayFontSizeProperty, value);
    }

    /// <summary>What to paint over the projected text, marker-pen style: typed terms and pointed-at appearances.</summary>
    public HighlightSet? Highlight
    {
        get => GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    public Logo? Logo
    {
        get => GetValue(LogoProperty);
        set => SetValue(LogoProperty, value);
    }

    public bool IsLiveOutput
    {
        get => GetValue(IsLiveOutputProperty);
        set => SetValue(IsLiveOutputProperty, value);
    }

    public IImage? LogoVideoFrame
    {
        get => GetValue(LogoVideoFrameProperty);
        set => SetValue(LogoVideoFrameProperty, value);
    }

    public Countdown? Countdown
    {
        get => GetValue(CountdownProperty);
        set => SetValue(CountdownProperty, value);
    }

    /// <summary>
    /// Draws the text without its shadow and outline, for the operator's
    /// small previews. The projector never sets this.
    /// </summary>
    public bool LowDetail
    {
        get => _lowDetail;
        set
        {
            if (_lowDetail == value)
                return;
            _lowDetail = value;
            Refresh();
        }
    }

    /// <summary>
    /// True while a media background (image or video) is showing behind this
    /// view: the theme's own background colour then stays transparent.
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

    /// <summary>Re-reads the slide's theme and re-renders. Call after editing themes.</summary>
    public void Refresh() => OnSlideChanged();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SlideProperty)
            OnSlideChanged();
        else if (change.Property == StateProperty)
            OnStateChanged(change.GetOldValue<OutputState>());
        else if (change.Property == OverlayProperty)
            OnOverlayChanged();
        else if (change.Property == OverlayFontFamilyProperty || change.Property == OverlayFontSizeProperty)
            ApplyOverlayFont();
        else if (change.Property == LogoProperty || change.Property == IsLiveOutputProperty
                 || change.Property == LogoVideoFrameProperty)
            RenderLogo();
        else if (change.Property == CountdownProperty)
            OnCountdownChanged();
        else if (change.Property == HighlightProperty)
            RenderText();
    }

    private SlideStyle Style => new(Slide);

    private SlideTheme CurrentTheme => Slide?.Theme ?? SlideTheme.Fallback;

    private void ApplyOverlayFont()
    {
        OverlayText.FontFamily = Fonts.Resolve(OverlayStyle.FontOrDefault(OverlayFontFamily));
        OverlayText.FontSize = OverlayStyle.ClampSize(OverlayFontSize);
    }

    // ── Cuadros de texto extra ───────────────────────────────────

    /// <summary>
    /// Draws the slide's extra text boxes. They are part of the slide's own
    /// design rather than the theme, so they are rebuilt whenever the slide
    /// changes; most slides have none and this does nothing.
    /// </summary>
    private void RenderExtraBoxes()
    {
        ExtraBoxLayer.Children.Clear();
        var style = Style;

        foreach (var box in Slide?.Override?.TextBoxes ?? [])
        {
            var text = new TextBlock
            {
                Text = Transform(box.Text) ?? string.Empty,
                FontFamily = Fonts.Resolve(string.IsNullOrWhiteSpace(box.FontFamily) ? style.FontFamily : box.FontFamily),
                FontSize = Math.Max(8, box.FontSize),
                FontWeight = box.Bold ? FontWeight.Bold : FontWeight.Normal,
                FontStyle = box.Italic ? FontStyle.Italic : FontStyle.Normal,
                Foreground = BrushFrom(box.Color, Brushes.White),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = Alignment(box.AlignH),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                // The box is a frame the text sits inside, exactly like the
                // main one: the vertical alignment decides where within it.
                VerticalAlignment = box.AlignV switch
                {
                    VAlign.Top => VerticalAlignment.Top,
                    VAlign.Bottom => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Center,
                },
            };

            var holder = new Grid { Width = Math.Max(20, box.Width), Height = Math.Max(20, box.Height) };
            holder.Children.Add(text);
            Canvas.SetLeft(holder, box.X);
            Canvas.SetTop(holder, box.Y);
            ExtraBoxLayer.Children.Add(holder);
        }
    }

    private void OnSlideChanged()
    {
        // The words on screen leave the way the new ones arrive; a copy of
        // them is taken before the new slide overwrites the text.
        var leaving = AnimateTransitions && State == OutputState.Content ? CopyOfText() : null;

        ApplyTheme();
        RenderText();
        RenderExtraBoxes();

        if (AnimateTransitions && State == OutputState.Content)
        {
            TransitionIn(TextLayer);
            TransitionIn(ExtraBoxLayer);
            if (leaving is not null)
                TransitionOut(leaving);
        }
    }

    // ── Tema ─────────────────────────────────────────────────────

    private void ApplyTheme()
    {
        var theme = CurrentTheme;
        var style = Style;

        RootCanvas.Background = theme.TransparentBackground || _overMedia
            ? Brushes.Transparent
            : BrushFromHex(theme.BackgroundColor, "#10141E");
        ApplyBackgroundImage(theme.BackgroundImagePath);
        ApplyThemeBackdrop();

        TextStack.HorizontalAlignment = style.AlignH switch
        {
            HAlign.Left => HorizontalAlignment.Left,
            HAlign.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center,
        };
        TextStack.VerticalAlignment = style.AlignV switch
        {
            VAlign.Top => VerticalAlignment.Top,
            VAlign.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center,
        };

        var fontFamily = Fonts.Resolve(style.FontFamily);
        var alignment = Alignment(style.AlignH);
        var foreground = BrushFromHex(style.TextColor, "#FFFFFF");

        MainText.FontFamily = fontFamily;
        MainText.FontWeight = style.Bold ? FontWeight.SemiBold : FontWeight.Normal;
        MainText.FontStyle = style.Italic ? FontStyle.Italic : FontStyle.Normal;
        MainText.Foreground = foreground;
        MainText.TextAlignment = alignment;
        MainText.TextDecorations = BuildDecorations(style);
        MainText.Effect = BuildOutline(style);
        MainTextShadow.Effect = BuildShadow(style);

        // La 2ª versión bíblica: mismo estilo que la principal, o el suyo.
        var matches = theme.SecondaryMatchesPrimary;
        SecondaryText.FontFamily = fontFamily;
        SecondaryText.FontWeight = matches && style.Bold ? FontWeight.SemiBold : FontWeight.Normal;
        SecondaryText.FontStyle = (matches ? style.Italic : theme.SecondaryItalic) ? FontStyle.Italic : FontStyle.Normal;
        SecondaryText.Foreground = matches ? foreground : BrushFromHex(theme.SecondaryColor, "#C9D4E8");
        SecondaryText.TextAlignment = alignment;
        SecondaryText.Effect = BuildOutline(style);
        SecondaryTextShadow.Effect = BuildShadow(style);
        SecondaryTextShadow.Margin = new Thickness(0, SecondarySpacing, 0, 0);

        CaptionLayer.Margin = new Thickness(theme.MarginHorizontal, theme.MarginVertical * 0.5,
            theme.MarginHorizontal, theme.MarginVertical * 0.5);
        CaptionText.FontFamily = Fonts.Resolve(theme.CaptionFontFamily ?? theme.FontFamily);
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
        UpdateBands();
    }

    /// <summary>
    /// The drop shadow behind the text, at the configured strength: as in WPF,
    /// 3 pixels down and to the right (315°), blurred as the theme says.
    /// </summary>
    private IEffect? BuildShadow(SlideStyle style)
    {
        var opacity = Math.Clamp(style.ShadowOpacity, 0, 1);
        if (LowDetail || !style.Shadow || opacity <= 0)
            return null;

        return new DropShadowDirectionEffect
        {
            BlurRadius = Math.Clamp(CurrentTheme.ShadowBlur, 0, 80),
            ShadowDepth = 3,
            Direction = 315,
            Color = Colors.Black,
            Opacity = opacity,
        };
    }

    /// <summary>
    /// The outline, built as BuildOutline in WPF: a tight, fully opaque halo
    /// of the outline colour hugging every letter, drawn as a shadow that does
    /// not move. (Plan B, a real stroke around the letters, is for batch 13.)
    /// </summary>
    private IEffect? BuildOutline(SlideStyle style)
    {
        var width = Math.Clamp(style.OutlineWidth, 0, 40);
        if (LowDetail || width <= 0)
            return null;

        return new DropShadowEffect
        {
            Color = ColorFromHex(style.OutlineColor, "#000000"),
            BlurRadius = width,
            OffsetX = 0,
            OffsetY = 0,
            Opacity = 1,
        };
    }

    private static TextDecorationCollection? BuildDecorations(SlideStyle style)
    {
        if (!style.Underline && !style.Strikethrough)
            return null;

        var decorations = new TextDecorationCollection();
        if (style.Underline)
            decorations.AddRange(TextDecorations.Underline);
        if (style.Strikethrough)
            decorations.AddRange(TextDecorations.Strikethrough);
        return decorations;
    }

    private void ApplyBackgroundImage(string? path)
    {
        if (path == _lastBackgroundImagePath)
            return;
        _lastBackgroundImagePath = path;
        BackgroundImage.Source = LoadBitmap(path);
    }

    /// <summary>
    /// Shows the theme's image and dimmer behind the words, unless a media is
    /// on screen: then the media wins and the image steps aside. The dimmer
    /// only stays up while the words are shown, so it can't darken the media
    /// alone.
    /// </summary>
    private void ApplyThemeBackdrop()
    {
        BackgroundImage.IsVisible = !_overMedia && BackgroundImage.Source != null;
        DimLayer.Opacity = _overMedia && State != OutputState.Content
            ? 0
            : Math.Clamp(CurrentTheme.BackgroundDim, 0, 1);
    }

    // ── Texto ────────────────────────────────────────────────────

    private void RenderText()
    {
        var style = Style;
        var main = Transform(Slide?.MainText);
        var secondary = Transform(Slide?.SecondaryText);

        // "Fit to width" keeps each written line unbroken; otherwise wrap.
        var wrapping = style.FitToWidth ? TextWrapping.NoWrap : TextWrapping.Wrap;
        MainText.TextWrapping = wrapping;
        SecondaryText.TextWrapping = wrapping;

        var fit = SlideTextFit.Fit(AvaloniaTextMetrics.Current, Slide, main, secondary, 1.0);
        TextLayer.Margin = new Thickness(fit.Margin.Left, fit.Margin.Top, fit.Margin.Right, fit.Margin.Bottom);
        MainText.FontSize = fit.FontSize;
        SecondaryText.FontSize = style.SecondarySize(fit.FontSize);

        MainText.LineHeight = style.LineSpacing is double spacing && spacing > 0
            ? fit.FontSize * spacing
            : double.NaN;

        RenderWithHighlight(MainText, main, isMain: true);
        RenderWithHighlight(SecondaryText, secondary, isMain: false);

        // The holder goes too, not just the text: its top margin would leave
        // a gap under every single-version verse.
        var hasSecondary = !string.IsNullOrEmpty(secondary);
        SecondaryText.IsVisible = hasSecondary;
        SecondaryTextShadow.IsVisible = hasSecondary;

        var hasCaption = !string.IsNullOrEmpty(Slide?.Caption);
        CaptionText.Text = Slide?.Caption ?? string.Empty;
        CaptionText.IsVisible = CurrentTheme.ShowCaption && hasCaption;

        UpdateBands();
    }

    private string? Transform(string? text) => SlideTextFit.ApplyCase(text, Style.Case);

    /// <summary>The text, with the words of the highlight painted as with a marker pen.</summary>
    private void RenderWithHighlight(TextBlock target, string? text, bool isMain)
    {
        target.Inlines?.Clear();
        target.Text = null;
        if (string.IsNullOrEmpty(text))
            return;

        // Pointed-at appearances belong to the main text only: their
        // position means nothing in the second version below it.
        var marked = (Highlight ?? HighlightSet.None).Resolve(text, includeSpans: isMain);
        if (marked.Count == 0)
        {
            target.Text = text;
            return;
        }

        var cuts = new SortedSet<int> { 0, text.Length };
        foreach (var mark in marked)
        {
            cuts.Add(mark.Start);
            cuts.Add(Math.Min(mark.End, text.Length));
        }

        var inlines = new InlineCollection();
        var edges = cuts.ToList();
        for (var i = 0; i + 1 < edges.Count; i++)
        {
            var piece = new TextSpan(edges[i], edges[i + 1] - edges[i]);
            var run = new Run(text.Substring(piece.Start, piece.Length));
            if (marked.Any(m => m.Contains(piece)))
            {
                run.Background = HighlightBrush;
                run.Foreground = Brushes.Black;
            }
            inlines.Add(run);
        }
        target.Inlines = inlines;
    }

    // ── Franjas ──────────────────────────────────────────────────

    /// <summary>
    /// The bands of colour behind each line (Temas → «Franjas»). Where each
    /// band goes is the shared <see cref="LineBandLayout"/>; here only the
    /// lines are read, and Avalonia says them straight away.
    /// </summary>
    private void UpdateBands()
    {
        var theme = CurrentTheme;
        DrawBands(MainBands, MainText, theme);
        DrawBands(SecondaryBands, SecondaryText, theme);
    }

    private void DrawBands(Canvas canvas, TextBlock text, SlideTheme theme)
    {
        canvas.Children.Clear();
        if (theme.LineBand == LineBand.None || !text.IsVisible || text.Bounds.Width <= 0
            || (string.IsNullOrEmpty(text.Text) && text.Inlines is not { Count: > 0 }))
            return;

        var lines = LineBandLayout.Lines(LineBoxes(text, canvas));

        // The edges of the screen, in the canvas's coordinates: the text layer
        // starts at its own margin from the left of the 1920 px canvas.
        var layerLeft = TextLayer.Margin.Left;
        var origin = canvas.TranslatePoint(new Point(0, 0), TextLayer) ?? default;
        var screenLeft = -(origin.X + layerLeft);
        var screenRight = CanvasWidth - (origin.X + layerLeft);

        var color = ColorFromHex(theme.LineBandColor, "#000000");
        var brush = new SolidColorBrush(color, Math.Clamp(theme.LineBandOpacity, 0, 1));

        foreach (var band in LineBandLayout.Bands(lines, theme.LineBand, text.FontSize, screenLeft, screenRight))
        {
            var rect = new Rectangle { Width = band.Width, Height = band.Height, Fill = brush };
            Canvas.SetLeft(rect, band.X);
            Canvas.SetTop(rect, band.Y);
            canvas.Children.Add(rect);
        }
    }

    /// <summary>
    /// The box of the words of every line of the text, in the canvas's
    /// coordinates: from its first letter to its last, without the space
    /// where it wraps.
    /// </summary>
    private static IEnumerable<BandBox> LineBoxes(TextBlock text, Visual canvas)
    {
        var top = text.Padding.Top;
        foreach (var line in text.TextLayout.TextLines)
        {
            var left = text.Padding.Left + line.Start;
            if (line.Width > 0 && text.TranslatePoint(new Point(left, top), canvas) is { } corner)
                yield return new BandBox(corner.X, corner.Y, line.Width, line.Height);
            top += line.Height;
        }
    }

    /// <summary>The lines of the main text as they were cut, for the benches and the tests.</summary>
    internal IReadOnlyList<string> MainLines() => LinesOf(MainText);

    internal static IReadOnlyList<string> LinesOf(TextBlock text)
    {
        var all = text.Inlines is { Count: > 0 } inlines
            ? string.Concat(inlines.OfType<Run>().Select(r => r.Text))
            : text.Text ?? string.Empty;
        return text.TextLayout.TextLines
            .Select(l => all.Substring(l.FirstTextSourceIndex, Math.Min(l.Length, all.Length - l.FirstTextSourceIndex)).Trim())
            .ToList();
    }

    // ── Logo ─────────────────────────────────────────────────────

    private string? _lastLogoImagePath;

    /// <summary>
    /// Paints the Logo state: a colour, plus an image (or the poster of a video
    /// logo) or free text. On the live output a video logo shows its moving
    /// picture (<see cref="LogoVideoFrame"/>).
    /// </summary>
    private void RenderLogo()
    {
        var logo = Logo;

        if (logo is null)
        {
            LogoLayer.Background = BrushFromHex("#10141E", "#10141E");
            LogoImage.IsVisible = false;
            LogoImage.Source = null;
            _lastLogoImagePath = null;
            MediaFraming.Apply(LogoImage, null, CanvasWidth, CanvasHeight);
            RenderLogoFill(null);
            LogoText.Text = "EcclesiaCast";
            LogoText.FontFamily = Fonts.Resolve("Segoe UI");
            LogoText.FontSize = 120;
            LogoText.FontWeight = FontWeight.SemiBold;
            LogoText.Foreground = Brushes.White;
            LogoText.IsVisible = true;
            return;
        }

        LogoLayer.Background = BrushFromHex(logo.BackgroundColor, "#10141E");

        LogoText.IsVisible = logo.Kind == LogoKind.Text;
        if (logo.Kind == LogoKind.Text)
        {
            LogoText.Text = logo.Text;
            LogoText.FontFamily = Fonts.Resolve(string.IsNullOrWhiteSpace(logo.FontFamily) ? "Segoe UI" : logo.FontFamily);
            LogoText.FontSize = Math.Clamp(logo.FontSize, 10, 400);
            LogoText.FontWeight = logo.Bold ? FontWeight.SemiBold : FontWeight.Normal;
            LogoText.Foreground = BrushFromHex(logo.TextColor, "#FFFFFF");
        }

        var path = logo.Kind switch
        {
            LogoKind.Image => logo.Path,
            LogoKind.Video => logo.PosterPath,
            _ => null,
        };

        if (logo.Kind == LogoKind.Video && LogoVideoFrame is { } frame)
        {
            // A path that never matches: the poster is loaded again when the video goes.
            _lastLogoImagePath = "\u0001video";
            LogoImage.Source = frame;
            LogoImage.IsVisible = true;
        }
        else if (path != _lastLogoImagePath)
        {
            _lastLogoImagePath = path;
            LogoImage.Source = LoadBitmap(path);
            LogoImage.IsVisible = LogoImage.Source is not null;
        }

        // Framed exactly like a background: zoom, shift, fixed size and fill.
        var framing = LogoImage.Source is null ? null : logo.AsFramedMedia();
        MediaFraming.Apply(LogoImage, framing, CanvasWidth, CanvasHeight);
        RenderLogoFill(framing);
    }

    /// <summary>
    /// The frame of a video logo was written again: Avalonia does not see the
    /// pixels of a bitmap change by itself.
    /// </summary>
    public void RedrawLogoVideo()
    {
        if (LogoImage.IsVisible && ReferenceEquals(LogoImage.Source, LogoVideoFrame))
            LogoImage.InvalidateVisual();
    }

    private string? _lastLogoFillKey;

    /// <summary>
    /// What fills the screen around a logo that does not cover it: the colour
    /// of the layer, or on top of it a blurred copy of the logo or an image of
    /// the media library. A video logo fills with a blurred still of itself.
    /// </summary>
    private void RenderLogoFill(MediaItem? framing)
    {
        var ownStill = framing is { Type: MediaType.Video, FillMediaId: MediaItem.FillWithSelfId }
                       && !string.IsNullOrWhiteSpace(framing.ThumbnailPath);
        var fill = framing is null || !MediaFraming.NeedsFill(framing) ? null
            : framing.FillsWithSelf || ownStill ? framing
            : framing.FillMediaId is int id and > 0 ? MediaLookup.Find(id) : null;

        // Only a still can be drawn here; a video fill shows its poster.
        var fillPath = fill is null ? null
            : ReferenceEquals(fill, framing) ? (ownStill ? framing!.ThumbnailPath : framing!.Path)
            : fill.Type == MediaType.Image ? fill.Path : fill.ThumbnailPath;
        var key = fillPath is null ? null : $"{(ReferenceEquals(fill, framing) ? "self" : "lib")}|{fillPath}";
        if (key == _lastLogoFillKey)
            return;
        _lastLogoFillKey = key;

        if (fillPath is null)
        {
            LogoFillImage.Source = null;
            LogoFillImage.IsVisible = false;
            return;
        }

        // The video's poster is loaded on its own: the logo image may be the moving picture.
        IImage? own = !ReferenceEquals(fill, framing) ? null
            : ownStill ? LoadBitmap(fillPath) : LogoImage.Source;
        LogoFillImage.Source = ReferenceEquals(fill, framing)
            ? MediaFraming.BlurredFill(own) ?? own
            : LoadBitmap(fillPath);
        LogoFillImage.IsVisible = LogoFillImage.Source is not null;
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

        var contentVisible = State == OutputState.Content;
        TextLayer.IsVisible = contentVisible;
        CaptionLayer.IsVisible = contentVisible;
        ExtraBoxLayer.IsVisible = contentVisible;
        ApplyThemeBackdrop();

        var blackTarget = State == OutputState.Black ? 1d : 0d;
        if (AnimateTransitions)
        {
            FadeLogoLayer(State == OutputState.Logo);
            _ = Fades.Fade(BlackLayer, blackTarget, TransitionSettings.Current.Black);

            if (State == OutputState.Content)
            {
                TransitionIn(TextLayer);
                TransitionIn(ExtraBoxLayer);
            }
            if (leaving is not null)
                TransitionOut(leaving);
        }
        else
        {
            _ = Fades.Fade(LogoLayer, 1, TimeSpan.Zero);
            LogoLayer.IsVisible = State == OutputState.Logo;
            _ = Fades.Fade(BlackLayer, blackTarget, TimeSpan.Zero);
        }
    }

    /// <summary>
    /// The logo comes in and goes out fading over the time set in «Transiciones».
    /// On the way out the layer is only taken off once it is invisible, and only
    /// if the logo has not been asked for again meanwhile.
    /// </summary>
    private async void FadeLogoLayer(bool show)
    {
        var duration = TransitionSettings.Current.Logo;
        var from = LogoLayer.IsVisible ? LogoLayer.Opacity : 0;

        if (show)
        {
            LogoLayer.IsVisible = true;
            await Fades.Fade(LogoLayer, from, 1, duration);
            return;
        }

        if (!LogoLayer.IsVisible)
            return;

        if (await Fades.Fade(LogoLayer, from, 0, duration) && State != OutputState.Logo)
        {
            LogoLayer.IsVisible = false;
            LogoLayer.Opacity = 1;
        }
    }

    /// <summary>
    /// The countdown redraws four times a second while it is up, and the
    /// timer is only alive during that time: every preview is one of these
    /// controls, and none of them should wake up for a screen nobody asked for.
    /// </summary>
    private async void OnCountdownChanged()
    {
        _countdownTicker ??= new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Normal,
            (_, _) => RenderCountdown());

        if (Countdown is null)
        {
            _countdownTicker.Stop();

            // Stopped, it fades away with the last time it showed.
            var gone = !(AnimateTransitions && CountdownLayer.IsVisible)
                       || await Fades.Fade(CountdownLayer, 0, MessageFade, Fades.EaseOut);
            if (gone && Countdown is null)
            {
                CountdownLayer.IsVisible = false;
                CountdownLayer.Opacity = 1;
            }
            return;
        }

        _countdownTicker.Start();
        RenderCountdown();

        _ = AnimateTransitions
            ? Fades.Fade(CountdownLayer, 0, 1, MessageFade, Fades.EaseOut)
            : Fades.Fade(CountdownLayer, 1, TimeSpan.Zero);
    }

    private void RenderCountdown()
    {
        if (Countdown is not { } countdown)
            return;

        CountdownLayer.IsVisible = true;
        var now = DateTimeOffset.Now;
        var finished = countdown.HasFinished(now);

        // Once it hits zero the heading has nothing left to introduce.
        CountdownHeading.Text = countdown.Heading ?? string.Empty;
        CountdownHeading.IsVisible = !finished && !string.IsNullOrWhiteSpace(countdown.Heading);

        var text = countdown.Format(now);
        if (CountdownClock.Text != text)
            CountdownClock.Text = text;

        // A closing message ("¡Bienvenidos!") is words, not digits: at the
        // clock's size it would run off both sides of the screen.
        CountdownClock.FontSize = finished ? 150 : 260;
        CountdownClock.TextWrapping = finished ? TextWrapping.Wrap : TextWrapping.NoWrap;
        CountdownClock.MaxWidth = finished ? CanvasWidth - 220 : double.PositiveInfinity;
    }

    private async void OnOverlayChanged()
    {
        if (!string.IsNullOrWhiteSpace(Overlay))
        {
            OverlayText.Text = Overlay;
            OverlayLayer.IsVisible = true;
            _ = AnimateTransitions
                ? Fades.Fade(OverlayLayer, 0, 1, MessageFade, Fades.EaseOut)
                : Fades.Fade(OverlayLayer, 1, TimeSpan.Zero);
            return;
        }

        // The message leaves the way it came: fading, with its words still on
        // it until it is gone.
        var gone = !(AnimateTransitions && OverlayLayer.IsVisible)
                   || await Fades.Fade(OverlayLayer, 0, MessageFade, Fades.EaseOut);
        if (gone && string.IsNullOrWhiteSpace(Overlay))
        {
            OverlayLayer.IsVisible = false;
            OverlayLayer.Opacity = 1;
            OverlayText.Text = string.Empty;
        }
    }

    // ── Transición entre diapositivas ────────────────────────────

    /// <summary>
    /// Brings the words in the way the theme asks (fade, slide left or up,
    /// zoom), in the theme's time. Whatever an earlier transition left behind
    /// goes first.
    /// </summary>
    private void TransitionIn(Control element)
    {
        var duration = TimeSpan.FromMilliseconds(Style.TransitionMs);
        var transition = CurrentTheme.Transition;

        element.RenderTransform = null;
        if (transition == SlideTransition.None || duration <= TimeSpan.Zero)
        {
            _ = Fades.Fade(element, 1, TimeSpan.Zero);
            return;
        }

        _ = Fades.Fade(element, 0, 1, duration, Fades.EaseOut);
        element.RenderTransformOrigin = RelativePoint.Center;

        switch (transition)
        {
            case SlideTransition.SlideLeft:
            {
                var move = new TranslateTransform();
                element.RenderTransform = move;
                _ = Fades.Animate(move, TranslateTransform.XProperty, CanvasWidth * 0.06, 0, duration, Fades.EaseOut);
                break;
            }

            case SlideTransition.SlideUp:
            {
                var move = new TranslateTransform();
                element.RenderTransform = move;
                _ = Fades.Animate(move, TranslateTransform.YProperty, CanvasHeight * 0.06, 0, duration, Fades.EaseOut);
                break;
            }

            case SlideTransition.Zoom:
            {
                var scale = new ScaleTransform(1.06, 1.06);
                element.RenderTransform = scale;
                _ = Fades.Animate(scale, ScaleTransform.ScaleXProperty, 1.06, 1, duration, Fades.EaseOut);
                _ = Fades.Animate(scale, ScaleTransform.ScaleYProperty, 1.06, 1, duration, Fades.EaseOut);
                break;
            }
        }
    }

    /// <summary>
    /// A still picture of the words on screen, to take them off with the same
    /// movement the next ones arrive with. Null when there is nothing to see
    /// leave, or when the theme cuts. (WPF copies the controls; Avalonia
    /// cannot, so the copy is a picture of them.)
    /// </summary>
    private Image? CopyOfText()
    {
        if (!TextLayer.IsVisible
            || (string.IsNullOrEmpty(MainText.Text) && MainText.Inlines is not { Count: > 0 })
            || CurrentTheme.Transition == SlideTransition.None
            || Style.TransitionMs <= 0
            || TextLayer.Bounds.Width < 1 || TextLayer.Bounds.Height < 1)
            return null;

        try
        {
            var size = new PixelSize((int)Math.Ceiling(TextLayer.Bounds.Width), (int)Math.Ceiling(TextLayer.Bounds.Height));
            var picture = new RenderTargetBitmap(size);
            picture.Render(TextLayer);
            return new Image
            {
                Source = picture,
                Width = TextLayer.Bounds.Width,
                Height = TextLayer.Bounds.Height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(TextLayer.Bounds.X, TextLayer.Bounds.Y, 0, 0),
                IsHitTestVisible = false,
                // Caught halfway through its own entrance, it leaves from there.
                Opacity = TextLayer.Opacity,
            };
        }
        catch (Exception)
        {
            // Without the copy the old words simply vanish.
            return null;
        }
    }

    /// <summary>
    /// Takes the copy of the old words off the screen: it fades and moves the
    /// opposite way to the arrival (out to the left, up, or shrinking a
    /// little), then goes away.
    /// </summary>
    private async void TransitionOut(Image copy)
    {
        var duration = TimeSpan.FromMilliseconds(Style.TransitionMs);
        RootCanvas.Children.Insert(RootCanvas.Children.IndexOf(TextLayer) + 1, copy);
        copy.RenderTransformOrigin = RelativePoint.Center;

        switch (CurrentTheme.Transition)
        {
            case SlideTransition.SlideLeft:
            {
                var move = new TranslateTransform();
                copy.RenderTransform = move;
                _ = Fades.Animate(move, TranslateTransform.XProperty, 0, -CanvasWidth * 0.06, duration, Fades.EaseIn);
                break;
            }

            case SlideTransition.SlideUp:
            {
                var move = new TranslateTransform();
                copy.RenderTransform = move;
                _ = Fades.Animate(move, TranslateTransform.YProperty, 0, -CanvasHeight * 0.06, duration, Fades.EaseIn);
                break;
            }

            case SlideTransition.Zoom:
            {
                var scale = new ScaleTransform(1, 1);
                copy.RenderTransform = scale;
                _ = Fades.Animate(scale, ScaleTransform.ScaleXProperty, 1, 0.94, duration, Fades.EaseIn);
                _ = Fades.Animate(scale, ScaleTransform.ScaleYProperty, 1, 0.94, duration, Fades.EaseIn);
                break;
            }
        }

        await Fades.Fade(copy, copy.Opacity, 0, duration, Fades.EaseIn);
        RootCanvas.Children.Remove(copy);
    }

    // ── Ayudantes ────────────────────────────────────────────────

    private static TextAlignment Alignment(HAlign align) => align switch
    {
        HAlign.Left => TextAlignment.Left,
        HAlign.Right => TextAlignment.Right,
        _ => TextAlignment.Center,
    };

    private static Bitmap? LoadBitmap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            using var stream = File.OpenRead(path);
            return Bitmap.DecodeToWidth(stream, 1920);
        }
        catch
        {
            // Ilegible: queda el color de fondo.
            return null;
        }
    }

    private static IBrush BrushFrom(string? hex, IBrush fallback) =>
        !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var color) ? new SolidColorBrush(color) : fallback;

    private static IBrush BrushFromHex(string? hex, string fallback) => new SolidColorBrush(ColorFromHex(hex, fallback));

    private static Color ColorFromHex(string? hex, string fallback) =>
        !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var color) ? color : Color.Parse(fallback);
}
