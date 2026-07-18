using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using Serilog;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Grabs a poster frame from a video with LibVLC. This is the fallback for the
/// files the Windows shell refuses to thumbnail — typically .mp4 files with a
/// QuickTime brand (what Premiere and After Effects export), which Explorer
/// shows with the generic player icon instead of a frame.
/// </summary>
public static class VideoThumbnail
{
    private const int ParseTimeoutMs = 4000;
    private const int FrameTimeoutMs = 8000;

    /// <summary>Returns a poster frame, or null if none could be decoded.</summary>
    public static BitmapSource? TryGet(LibVLC libVlc, string sourcePath, int width, int height)
    {
        var buffer = IntPtr.Zero;
        try
        {
            using var media = new Media(libVlc, new Uri(sourcePath));
            media.AddOption(":no-audio");
            media.AddOption(":no-spu");
            // The shell already failed on these files; software decoding is the
            // most permissive path and a single frame doesn't need speed.
            media.AddOption(":avcodec-hw=none");

            if (media.Parse(MediaParseOptions.ParseLocal, ParseTimeoutMs).GetAwaiter().GetResult()
                != MediaParsedStatus.Done)
            {
                Log.Debug("VLC no pudo analizar {Path} para la miniatura", sourcePath);
                return null;
            }

            // Seek a bit into the video: the first frames are often black or a fade-in.
            var seekSeconds = Math.Clamp(media.Duration / 1000.0 * 0.15, 0, 10);
            if (seekSeconds >= 1)
                media.AddOption(FormattableString.Invariant($":start-time={seekSeconds:0.###}"));

            var (w, h) = FitToBox(media, width, height);
            var pitch = (uint)(w * 4);
            var frameSize = (int)pitch * h;
            buffer = Marshal.AllocHGlobal(frameSize);

            byte[]? frame = null;
            using var ready = new ManualResetEventSlim(false);

            using var player = new MediaPlayer(libVlc);
            player.SetVideoFormat("RV32", (uint)w, (uint)h, pitch);

            // Locals, kept alive past Stop() below: the native side keeps calling
            // these until playback actually stops, and letting the GC collect
            // them mid-playback is what caused the background-video crash.
            MediaPlayer.LibVLCVideoLockCb lockCb = (_, planes) =>
            {
                Marshal.WriteIntPtr(planes, buffer);
                return IntPtr.Zero;
            };
            MediaPlayer.LibVLCVideoUnlockCb unlockCb = (_, _, _) => { };
            MediaPlayer.LibVLCVideoDisplayCb displayCb = (_, _) =>
            {
                // Called with a complete picture; VLC won't touch the buffer
                // again until the next lock, so copying here is safe.
                if (frame is not null)
                    return;
                var copy = new byte[frameSize];
                Marshal.Copy(buffer, copy, 0, frameSize);
                frame = copy;
                ready.Set();
            };
            player.SetVideoCallbacks(lockCb, unlockCb, displayCb);

            player.Play(media);
            var got = ready.Wait(FrameTimeoutMs);
            player.Stop();

            GC.KeepAlive(lockCb);
            GC.KeepAlive(unlockCb);
            GC.KeepAlive(displayCb);

            if (!got || frame is null)
            {
                Log.Debug("VLC no entregó ningún cuadro de {Path} en {Timeout} ms", sourcePath, FrameTimeoutMs);
                return null;
            }

            // RV32 is BGRX: the fourth byte is padding, so read it as Bgr32 and
            // the thumbnail comes out opaque instead of fully transparent.
            var source = BitmapSource.Create(
                w, h, 96, 96, PixelFormats.Bgr32, null, frame, (int)pitch);
            source.Freeze();
            return source;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Falló la extracción de miniatura con VLC para {Path}", sourcePath);
            return null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Scales the video's own size into the requested box, keeping its aspect ratio.</summary>
    private static (int Width, int Height) FitToBox(Media media, int boxWidth, int boxHeight)
    {
        var video = media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
        var sourceWidth = (int)video.Data.Video.Width;
        var sourceHeight = (int)video.Data.Video.Height;
        if (sourceWidth <= 0 || sourceHeight <= 0)
            return (boxWidth, boxHeight);

        var scale = Math.Min(boxWidth / (double)sourceWidth, boxHeight / (double)sourceHeight);
        // Even dimensions keep the decoder's chroma planes happy.
        var width = Math.Max(2, (int)Math.Round(sourceWidth * scale / 2) * 2);
        var height = Math.Max(2, (int)Math.Round(sourceHeight * scale / 2) * 2);
        return (width, height);
    }
}
