using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EcclesiaCast.Presentation.Rendering;
using EcclesiaCast.Presentation.ViewModels;
using EcclesiaCast.Core.Stage;

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

        var single = StageBoxText.IsSingleLine(Box.Kind);

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

        var text = StageBoxText.For(Box, Projection);
        _text.Text = text;

        // The section timer turns red once its time is over.
        if (Box.Kind == StageBoxKind.SectionTimer)
        {
            var late = StageBoxText.IsLate(Box, Projection);
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
        if (StageBoxText.MovesEverySecond(Box, Projection))
            Refresh();
    }

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
}
