using System.Diagnostics;
using System.IO;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Turns an animated GIF into a looping MP4 kept beside the rest of the
/// church's data, with ffmpeg. The GIF itself stays where it was; the library
/// gets the video, which the program already knows how to loop, mute and show.
/// Without ffmpeg nothing is converted and the GIF stays a still picture.
/// </summary>
public static class GifImporter
{
    private static string Root => Path.Combine(App.DataDir, "gifs");

    /// <summary>A long GIF can take a while to encode; past this it is left as a picture.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    public static bool IsAvailable => YtDlp.FindFfmpeg() is not null;

    /// <summary>True when this is a video the program made from a GIF (it goes with its library item).</summary>
    public static bool IsConverted(string path) =>
        path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Makes the video of <paramref name="gifPath"/>; returns its path, or null
    /// when ffmpeg is missing or failed (the caller keeps the picture).
    /// </summary>
    public static async Task<string?> ConvertAsync(string gifPath, CancellationToken cancellation = default)
    {
        var ffmpeg = YtDlp.FindFfmpeg();
        if (ffmpeg is null)
            return null;

        var target = Path.Combine(Root, Guid.NewGuid().ToString("N") + ".mp4");

        // Written under another name and renamed when done: a conversion cut
        // short (the program closed) never leaves a half video in the library.
        var partial = Path.Combine(Root, Path.GetFileNameWithoutExtension(target) + ".part.mp4");

        try
        {
            Directory.CreateDirectory(Root);

            var info = new ProcessStartInfo(ffmpeg)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var argument in GifFile.ToVideoArguments(gifPath, partial))
                info.ArgumentList.Add(argument);

            using var process = Process.Start(info);
            if (process is null)
                return null;

            // Below normal: the operator may be projecting while a GIF is brought in.
            try { process.PriorityClass = ProcessPriorityClass.BelowNormal; }
            catch { /* ya terminó */ }

            var output = process.StandardOutput.ReadToEndAsync(cancellation);
            var errors = process.StandardError.ReadToEndAsync(cancellation);

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            limit.CancelAfter(Timeout);
            try
            {
                await process.WaitForExitAsync(limit.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ya terminó */ }
                Log.Warning("La conversión del GIF {Path} tardó demasiado o se canceló", gifPath);
                return null;
            }

            await output;
            var message = await errors;
            if (process.ExitCode != 0 || !File.Exists(partial))
            {
                Log.Warning("ffmpeg no pudo convertir el GIF {Path} ({Exit}): {Output}", gifPath, process.ExitCode, message.Trim());
                return null;
            }

            File.Move(partial, target, overwrite: true);
            Log.Information("GIF {Path} convertido en video {Target}", gifPath, target);
            return target;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Log.Warning(ex, "No se pudo convertir el GIF {Path}", gifPath);
            return null;
        }
        finally
        {
            try { File.Delete(partial); } catch { /* puede no existir */ }
        }
    }

    /// <summary>Takes a converted video away when its library item is removed.</summary>
    public static void Forget(string videoPath)
    {
        try
        {
            File.Delete(videoPath);
        }
        catch (IOException ex)
        {
            Log.Debug(ex, "No se pudo borrar el video del GIF {Path}", videoPath);
        }
    }
}
