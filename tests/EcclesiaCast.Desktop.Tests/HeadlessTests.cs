using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The ground every other test stands on: drawing in memory works.</summary>
public sealed class HeadlessTests
{
    [AvaloniaFact]
    public void A_full_screen_frame_is_drawn_in_memory_with_skia()
    {
        var window = new Window
        {
            Width = 1920,
            Height = 1080,
            Content = new Border { Background = Brushes.Red },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        using var frame = window.CaptureRenderedFrame();
        window.Close();

        Assert.NotNull(frame);
        Assert.Equal(1920, frame.PixelSize.Width);
        Assert.Equal(1080, frame.PixelSize.Height);
    }
}
