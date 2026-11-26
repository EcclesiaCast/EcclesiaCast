using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Presentation.Platform;
using LibVLCSharp.Shared;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The video with LibVLC for real, drawn in memory: a 3-second test pattern
/// (<c>tests/medios/prueba-3s.mp4</c>) plays for two seconds, jumping near
/// its end so the loop starts over inside them. Where LibVLC cannot be loaded
/// (a Mac runner without it) the test is skipped and says why.
/// </summary>
public sealed class VideoSurfaceTests
{
    private static string TestVideo => VideoEngineForTests.TestVideo;

    [AvaloniaFact]
    public void A_looping_video_draws_its_frames_and_the_loop_never_shows_black()
    {
        if (VideoEngineForTests.Engine is not { } engine)
        {
            Assert.Skip("LibVLC no se pudo cargar en esta máquina: la prueba del video con VLC de verdad no corre acá.");
            return;
        }

        var before = PlatformServices.Video;
        PlatformServices.Video = new VideoEngine(engine);
        var surface = new VlcVideoSurface { HoldFade = TimeSpan.Zero };
        var window = new Window { Width = 640, Height = 360, Content = surface };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var brightness = new List<double>();
            var empties = 0;
            var firstFrames = 0;
            surface.FirstFrame += (_, _) => firstFrames++;
            surface.FrameWritten += (_, _) =>
            {
                if (surface.Surface.Source is WriteableBitmap bitmap)
                    brightness.Add(Brightness(bitmap));
            };

            surface.Show(new MediaItem
            {
                Id = 1,
                Name = "prueba",
                Type = MediaType.Video,
                Path = TestVideo,
                EndBehavior = VideoEndBehavior.Loop,
                Muted = true,
            });

            // The first time VLC runs on a machine it looks through its plugins
            // before opening anything: the first frame is waited for apart.
            var opening = Stopwatch.StartNew();
            while (surface.FramesDrawn == 0 && opening.Elapsed < TimeSpan.FromSeconds(20))
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
            }
            Assert.True(surface.FramesDrawn > 0, "El video no dibujó ningún cuadro en 20 segundos.");

            // Two seconds of playing, the loop included: it jumps to 2.4 s, so
            // it starts over 0.6 s later.
            var drawnBefore = surface.FramesDrawn;
            surface.SeekTo(TimeSpan.FromSeconds(2.4));
            var clock = Stopwatch.StartNew();
            var positions = new List<TimeSpan>();
            while (clock.Elapsed < TimeSpan.FromSeconds(2))
            {
                Dispatcher.UIThread.RunJobs();
                if (surface.Surface.Source is null && !surface.IsHolding)
                    empties++;
                positions.Add(surface.State.Position);
                Thread.Sleep(5);
            }

            var drawn = surface.FramesDrawn - drawnBefore;
            Assert.True(drawn >= 15, $"Llegaron sólo {drawn} cuadros en dos segundos.");
            Assert.True(StartedOver(positions), "El video no volvió a empezar en los dos segundos.");
            Assert.Equal(1, firstFrames);
            Assert.Equal(0, empties);
            Assert.True(brightness.Min() > 30, $"Un cuadro salió casi negro (brillo {brightness.Min():0}).");
        }
        finally
        {
            surface.Stop();
            window.Close();
            Dispatcher.UIThread.RunJobs();
            PlatformServices.Video = before;
        }
    }

    [AvaloniaFact]
    public void Without_an_engine_a_video_is_simply_not_shown()
    {
        var before = PlatformServices.Video;
        PlatformServices.Video = null;
        try
        {
            var surface = new VlcVideoSurface();
            surface.Show(new MediaItem { Id = 1, Type = MediaType.Video, Path = TestVideo });
            Dispatcher.UIThread.RunJobs();

            Assert.Null(surface.Surface.Source);
            Assert.Equal(PlaybackState.None, surface.State);
        }
        finally
        {
            PlatformServices.Video = before;
        }
    }

    /// <summary>True when the position went back once it had passed two seconds.</summary>
    private static bool StartedOver(List<TimeSpan> positions)
    {
        var past = false;
        foreach (var position in positions)
        {
            if (position > TimeSpan.FromSeconds(2))
                past = true;
            else if (past && position < TimeSpan.FromSeconds(1))
                return true;
        }
        return false;
    }

    /// <summary>The average of 64 points of the picture, 0–255.</summary>
    private static unsafe double Brightness(WriteableBitmap bitmap)
    {
        using var pixels = bitmap.Lock();
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        long sum = 0;
        for (var i = 0; i < 64; i++)
        {
            var x = (i % 8 * 2 + 1) * width / 16;
            var y = (i / 8 * 2 + 1) * height / 16;
            var p = (byte*)pixels.Address + (long)y * pixels.RowBytes + x * 4;
            sum += p[0] + p[1] + p[2];
        }
        return sum / (64.0 * 3);
    }
}

/// <summary>One LibVLC for every test that plays a video; null where it cannot be loaded.</summary>
internal static class VideoEngineForTests
{
    private static readonly Lazy<LibVLC?> Lazy = new(VlcEngine.Start);

    public static LibVLC? Engine => Lazy.Value;

    /// <summary>The 3-second test pattern (tests/medios/prueba-3s.mp4).</summary>
    public static string TestVideo => Path.Combine(AppContext.BaseDirectory, "medios", "prueba-3s.mp4");
}
