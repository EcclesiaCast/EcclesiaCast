using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Rendering;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// One rectangle of the stage screen, drawn from its <see cref="StageBox"/>,
/// in Avalonia: the same control as the WPF <c>StageBoxView</c>. What it says
/// is decided by the shared <see cref="StageBoxText"/>; this only draws it.
/// With no projection (the editor) it shows a sample text instead.
/// </summary>
public sealed class StageBoxView : Border
{
    private static readonly IBrush LateBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x5A, 0x5A));
    private static readonly IBrush OverlayBand = new SolidColorBrush(Color.FromRgb(0x1B, 0x24, 0x36));
    private static readonly IBrush Blue = new SolidColorBrush(Color.FromRgb(0x2F, 0x63, 0xC9));

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

    /// <summary>The text of the box, for the benches and the tests; null for the mirror.</summary>
    internal TextBlock? Text => _text;

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
            // As in WPF, letters that go past the frame are not cut.
            ClipToBounds = false,
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
        // Without a typeface of its own the text has the one WPF inherits: Segoe UI.
        AvaloniaFontCatalog.Current
            .Face(string.IsNullOrWhiteSpace(Box.FontFamily) ? "Segoe UI" : Box.FontFamily,
                Box.Bold ? SlideTextFit.BoldWeight : SlideTextFit.NormalWeight, false)
            .ApplyTo(_text);
        if (!single)
            _text.MaxWidth = Math.Max(20, Box.Width - Padding.Left - Padding.Right);

        // Nobody can scroll this screen, so a text that does not fit has to
        // get smaller rather than be cut off unnoticed.
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
            _text.Foreground = StageBoxText.IsLate(Box, Projection) ? LateBrush : ParseBrush(Box.Color);

        // The footer notice only has its band while there is something in it.
        if (Box.Kind == StageBoxKind.Overlay)
        {
            var visible = !string.IsNullOrWhiteSpace(text);
            Background = visible ? OverlayBand : null;
            BorderBrush = Blue;
            BorderThickness = visible ? new Thickness(0, 3, 0, 0) : new Thickness(0);
        }
    }

    /// <summary>Once a second: what moves on its own is redrawn.</summary>
    public void Tick()
    {
        if (StageBoxText.MovesEverySecond(Box, Projection))
            Refresh();
    }

    /// <summary>
    /// The projector's own picture: the projected view, with its background,
    /// fill and logo, and the video as the small copy of the projector's
    /// frames (<see cref="ProjectionViewModel.LiveFrame"/>). Whoever hands
    /// that copy out (the window service, batch 11) also asks the views to
    /// draw it again on every frame.
    /// </summary>
    private Control BuildMirror()
    {
        if (Projection is not { } vm)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x2A)),
                BorderBrush = Blue,
                BorderThickness = new Thickness(2),
                Child = new TextBlock
                {
                    Text = "Igual a la pantalla principal",
                    FontFamily = AvaloniaFontCatalog.Current.Resolve("Segoe UI"),
                    Foreground = Brushes.White,
                    FontSize = 54,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
        }

        var view = new ProjectedView { Width = StageLayout.CanvasWidth, Height = StageLayout.CanvasHeight };
        void Copy()
        {
            view.Slide = vm.Slide;
            view.State = vm.State;
            view.Overlay = vm.Overlay;
            view.Countdown = vm.Countdown;
            view.Highlight = vm.Highlight;
            view.BackgroundMedia = vm.Background;
            view.FillMedia = vm.BackgroundFill;
            view.BackgroundBlur = vm.BackgroundBlur;
            view.LiveVideoFrame = vm.LiveFrame as IImage;
            view.LogoVideoFrame = vm.LiveLogoFrame as IImage;
            view.Logo = vm.ActiveLogo;
        }

        Copy();
        PropertyChangedEventHandler follow = (_, _) => Copy();
        // The copies are written in place: drawn again on every frame.
        EventHandler video = (_, _) => view.RedrawLiveVideo();
        EventHandler logo = (_, _) => view.RedrawLogoVideo();
        vm.PropertyChanged += follow;
        LiveFrames.VideoWritten += video;
        LiveFrames.LogoWritten += logo;
        DetachedFromVisualTree += (_, _) =>
        {
            vm.PropertyChanged -= follow;
            LiveFrames.VideoWritten -= video;
            LiveFrames.LogoWritten -= logo;
        };

        // The slide is laid out on 1920×1080 whatever the size of the box, so
        // it is drawn at that size and scaled down into the rectangle.
        return new Viewbox { Stretch = Stretch.Uniform, Child = view };
    }

    private static IBrush ParseBrush(string? hex) =>
        // A damaged colour in a saved layout falls back to white.
        !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var color) ? new SolidColorBrush(color) : Brushes.White;
}
