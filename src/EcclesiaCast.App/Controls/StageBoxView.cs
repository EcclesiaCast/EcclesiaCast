using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// One rectangle of the stage screen, drawn from its <see cref="StageBox"/>.
/// The stage screen builds one per box with the live projection behind it;
/// the editor builds the same control with no projection, and it shows a
/// sample text instead, so what the operator arranges is what the stage gets.
/// </summary>
public sealed class StageBoxView : Border
{
    private TextBlock? _text;

    public StageBoxView(StageBox box, ProjectionViewModel? projection)
    {
        Box = box;
        Projection = projection;
        ClipToBounds = true;
        Apply();
    }

    public StageBox Box { get; }

    /// <summary>The live projection, or null in the editor (sample mode).</summary>
    public ProjectionViewModel? Projection { get; }

    /// <summary>Rebuilds the box from its settings: kind, size, font, colour, alignment.</summary>
    public void Apply()
    {
        Width = Box.Width;
        Height = Box.Height;
        Canvas.SetLeft(this, Box.X);
        Canvas.SetTop(this, Box.Y);
        Padding = new Thickness(8, 4, 8, 4);

        _text = null;
        Child = null;
        Background = null;
        BorderThickness = new Thickness(0);

        if (Box.Kind == StageBoxKind.Mirror)
        {
            Padding = new Thickness(0);
            Child = BuildMirror();
            return;
        }

        var single = Box.Kind is StageBoxKind.Clock or StageBoxKind.Timer or StageBoxKind.Countdown
            or StageBoxKind.SectionTimer or StageBoxKind.LiveName or StageBoxKind.Overlay;

        _text = new TextBlock
        {
            FontSize = Box.FontSize,
            FontWeight = Box.Bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = ParseBrush(Box.Color),
            TextAlignment = Box.Align switch
            {
                StageAlign.Left => TextAlignment.Left,
                StageAlign.Right => TextAlignment.Right,
                _ => TextAlignment.Center,
            },
            // A short line is measured as wide as it needs, so the Viewbox can
            // shrink it when it is too wide; text that wraps is held to the
            // width of the box and shrinks only when it gets too tall.
            TextWrapping = single ? TextWrapping.NoWrap : TextWrapping.Wrap,
        };
        // Without a typeface the text keeps the one it inherits, as before.
        if (!string.IsNullOrWhiteSpace(Box.FontFamily))
            _text.FontFamily = new FontFamily(Box.FontFamily);
        if (!single)
            _text.MaxWidth = Math.Max(20, Box.Width - Padding.Left - Padding.Right);

        // Nobody can scroll this screen (no mouse, no focus), so a text that
        // does not fit has to get smaller rather than be cut off unnoticed.
        Child = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = Box.Align switch
            {
                StageAlign.Left => HorizontalAlignment.Left,
                StageAlign.Right => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center,
            },
            VerticalAlignment = Box.Kind is StageBoxKind.Notes ? VerticalAlignment.Top : VerticalAlignment.Center,
            Child = _text,
        };

        Refresh();
    }

    /// <summary>Redraws the content after something in the projection changed.</summary>
    public void Refresh()
    {
        if (_text is null)
            return;

        var text = Projection is { } vm ? LiveContent(vm) : SampleContent();
        _text.Text = text;

        // The section timer turns red once its time is over.
        if (Box.Kind == StageBoxKind.SectionTimer)
        {
            var late = Projection?.Clocks is { HasSection: true } clocks && clocks.Section.IsOvertime(DateTimeOffset.Now);
            _text.Foreground = late ? new SolidColorBrush(Color.FromRgb(0xFF, 0x5A, 0x5A)) : ParseBrush(Box.Color);
        }

        // The footer notice only has its band while there is something in it.
        if (Box.Kind == StageBoxKind.Overlay)
        {
            var visible = !string.IsNullOrWhiteSpace(text);
            Background = visible ? new SolidColorBrush(Color.FromRgb(0x1B, 0x24, 0x36)) : null;
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x63, 0xC9));
            BorderThickness = visible ? new Thickness(0, 3, 0, 0) : new Thickness(0);
        }
    }

    /// <summary>
    /// Once a second: the clock, the stopwatch and the countdown move on their
    /// own, and while a countdown runs it replaces the words on the other boxes.
    /// </summary>
    public void Tick()
    {
        if (Box.Kind is StageBoxKind.Clock or StageBoxKind.Timer or StageBoxKind.Countdown or StageBoxKind.SectionTimer
            || Projection?.Countdown is not null)
            Refresh();
    }

    // ── What each kind shows ─────────────────────────────────────

    private string LiveContent(ProjectionViewModel vm)
    {
        var now = DateTimeOffset.Now;

        // While a countdown runs it is what the congregation sees, so it is
        // what the band and whoever is preaching need to see too: they are
        // the ones who have to be in place when it hits zero.
        if (vm.Countdown is { } countdown)
        {
            switch (Box.Kind)
            {
                case StageBoxKind.LiveText:
                    return countdown.Format(now);
                case StageBoxKind.LiveName:
                    return "⏱  CUENTA REGRESIVA";
                case StageBoxKind.NextSlide:
                    return countdown.HasFinished(now) || string.IsNullOrWhiteSpace(countdown.Heading)
                        ? "—"
                        : countdown.Heading;
            }
        }

        return Box.Kind switch
        {
            // Black and Clear hide the words from the congregation, but the
            // stage still needs to read them: that is what a stage display is for.
            StageBoxKind.LiveText => Transform(vm.Slide?.MainText ?? string.Empty, vm.Slide?.Theme),
            StageBoxKind.NextSlide => vm.NextSlide is { } next ? Transform(next.MainText, next.Theme) : "—",
            StageBoxKind.LiveName => vm.State switch
            {
                OutputState.Black => "⏸  PANTALLA EN NEGRO",
                OutputState.Logo => "🖼  LOGO",
                OutputState.Clear => vm.Slide is null ? "—" : "👁  SIN TEXTO EN PANTALLA",
                _ => vm.SlideLabel ?? string.Empty,
            },
            StageBoxKind.Clock => DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture),
            StageBoxKind.Timer => TimeText.Elapsed(vm.Clocks.Stopwatch.Elapsed(now)),
            StageBoxKind.SectionTimer => vm.Clocks.HasSection ? TimeText.Clock(vm.Clocks.Section.Remaining(now)) : "—",
            StageBoxKind.Countdown => vm.Countdown?.Format(now) ?? string.Empty,
            StageBoxKind.Notes => vm.StageNotes ?? string.Empty,
            StageBoxKind.Overlay => vm.Overlay ?? string.Empty,
            StageBoxKind.FixedText => Box.Text,
            _ => string.Empty,
        };
    }

    private string SampleContent() => Box.Kind switch
    {
        StageBoxKind.LiveText => "Santo, santo, santo, Señor omnipotente,\nsiempre el labio mío loores te dará.",
        StageBoxKind.NextSlide => "Santo, santo, santo, te adoro reverente",
        StageBoxKind.LiveName => "Coro",
        StageBoxKind.Clock => "10:30",
        StageBoxKind.Timer => "12:45",
        StageBoxKind.Countdown => "5:00",
        StageBoxKind.SectionTimer => "25:00",
        StageBoxKind.Notes => "Los puntos del mensaje:\n1. La gracia\n2. La fe\n3. La respuesta",
        StageBoxKind.Overlay => "Aviso al pie",
        StageBoxKind.FixedText => string.IsNullOrEmpty(Box.Text) ? "Texto fijo" : Box.Text,
        _ => string.Empty,
    };

    private UIElement BuildMirror()
    {
        // The projector's own picture: the live slide on its background, the
        // same control the operator's LIVE panel uses.
        if (Projection is not { } vm)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x2A)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x63, 0xC9)),
                BorderThickness = new Thickness(2),
                Child = new TextBlock
                {
                    Text = "Igual a la pantalla principal",
                    Foreground = Brushes.White,
                    FontSize = 54,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
        }

        var view = new ProjectedView();
        Bind(view, ProjectedView.SlideProperty, nameof(ProjectionViewModel.Slide), vm);
        Bind(view, ProjectedView.StateProperty, nameof(ProjectionViewModel.State), vm);
        Bind(view, ProjectedView.OverlayProperty, nameof(ProjectionViewModel.Overlay), vm);
        Bind(view, ProjectedView.CountdownProperty, nameof(ProjectionViewModel.Countdown), vm);
        Bind(view, ProjectedView.HighlightProperty, nameof(ProjectionViewModel.Highlight), vm);
        Bind(view, ProjectedView.BackgroundMediaProperty, nameof(ProjectionViewModel.Background), vm);
        Bind(view, ProjectedView.FillMediaProperty, nameof(ProjectionViewModel.BackgroundFill), vm);
        Bind(view, ProjectedView.BackgroundBlurProperty, nameof(ProjectionViewModel.BackgroundBlur), vm);
        Bind(view, ProjectedView.LiveVideoFrameProperty, nameof(ProjectionViewModel.LiveFrame), vm);
        Bind(view, ProjectedView.LogoProperty, nameof(ProjectionViewModel.ActiveLogo), vm);

        // The slide is laid out on 1920×1080 whatever the size of the box, so
        // it is drawn at that size and scaled down into the rectangle.
        view.Width = StageLayout.CanvasWidth;
        view.Height = StageLayout.CanvasHeight;
        return new Viewbox { Stretch = Stretch.Uniform, Child = view };
    }

    private static void Bind(DependencyObject target, DependencyProperty property, string path, object source) =>
        BindingOperations.SetBinding(target, property, new Binding(path) { Source = source });

    // ── Helpers ──────────────────────────────────────────────────

    private static Brush ParseBrush(string? hex)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex) && ColorConverter.ConvertFromString(hex) is Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
        }
        catch (FormatException)
        {
            // A damaged colour in a saved layout falls back to white.
        }

        return Brushes.White;
    }

    /// <summary>
    /// Matches the projector's casing so the singers read what the
    /// congregation reads; everything else about the look stays plain.
    /// </summary>
    private static string Transform(string text, SlideTheme? theme) =>
        (theme?.TextCase ?? TextCase.None) switch
        {
            TextCase.Upper => text.ToUpper(CultureInfo.CurrentCulture),
            TextCase.Lower => text.ToLower(CultureInfo.CurrentCulture),
            TextCase.Title => CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(text.ToLower(CultureInfo.CurrentCulture)),
            _ => text,
        };
}
