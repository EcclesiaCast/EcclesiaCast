using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// Draws one case of tests/casos-de-render in memory at 1920×1080, the way
/// the WPF bench does: black behind everything, as the projector has when no
/// video is there. The window is headless: nothing is shown.
/// </summary>
internal sealed class SlideDrawing : IDisposable
{
    public const int Width = 1920;
    public const int Height = 1080;

    private readonly Window _window;

    public SlideDrawing(Control content)
    {
        var host = new Grid { Width = Width, Height = Height, Background = Brushes.Black, ClipToBounds = true };
        host.Children.Add(content);
        _window = new Window { Width = Width, Height = Height, Content = host };
        _window.Show();
        Settle();
    }

    /// <summary>The slide of a case in one of its themes, as the WPF bench builds it.</summary>
    public static SlideView Slide(RenderCase c, RenderTheme theme)
    {
        var view = new SlideView { AnimateTransitions = false };
        if (c.Overlay is { } overlay)
        {
            view.OverlayFontFamily = c.OverlayFont(theme);
            view.OverlayFontSize = overlay.FontSize;
            view.Overlay = overlay.Text;
        }

        view.Slide = c.BuildSlide(theme);
        view.Countdown = c.BuildCountdown(DateTimeOffset.Now);
        if (c.BuildLogo(theme) is { } logo)
            view.Logo = logo;
        view.State = c.State;
        return view;
    }

    /// <summary>Lets the layout, the bands and the drawing catch up.</summary>
    public void Settle()
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    public WriteableBitmap Capture()
    {
        Settle();
        return _window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was drawn.");
    }

    public void Save(string folder, string name)
    {
        using var frame = Capture();
        frame.Save(Path.Combine(folder, name + ".png"), PngBitmapEncoderOptions.Default);
    }

    public void Dispose() => _window.Close();
}
