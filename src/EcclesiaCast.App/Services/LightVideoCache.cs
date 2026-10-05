using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Keeps light copies of the heavy videos (4K, or 50/60 frames a second) and
/// plays them instead of the originals. The library never changes: every
/// item keeps pointing at the original file, and a copy is only ever a
/// stand-in found by looking in this folder. Delete the folder and the
/// program simply plays the originals again and remakes the copies.
///
/// A copy is named after the original's path, size and last change, so a
/// re-exported original gets a fresh copy instead of a stale one.
/// </summary>
public static class LightVideoCache
{
    /// <summary>Local, not roaming, and regenerable: a cache, not user data.</summary>
    public static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EcclesiaCast", "copias-livianas");

    /// <summary>
    /// A copy at 1080p looks like the original only while it is not zoomed
    /// in; past this the original is played so a close-up stays sharp.
    /// </summary>
    private const double MaxZoomForCopy = 1.05;

    private static readonly object Sync = new();
    private static readonly Queue<string> Pending = new();
    private static readonly HashSet<string> Queued = new(StringComparer.OrdinalIgnoreCase);
    private static Task? _worker;
    private static Func<bool> _isBusy = () => false;

    /// <summary>The copy being written right now, which the clean-up must leave alone.</summary>
    private static volatile string? _converting;

    /// <summary>The ffmpeg doing the current copy, so closing the program stops it too.</summary>
    private static volatile Process? _encoder;

    /// <summary>
    /// Stops a conversion under way. Left running, ffmpeg would outlive the
    /// program; the half-written copy is thrown away on the next start.
    /// </summary>
    public static void Shutdown()
    {
        try { _encoder?.Kill(); } catch { /* ya terminó */ }
    }

    /// <summary>Short messages for the status bar ("Preparando copia liviana de…").</summary>
    public static event Action<string>? StatusChanged;

    /// <summary>
    /// The file to play for this item: its light copy when one is ready,
    /// otherwise the original. Cheap enough to call on every Show.
    /// </summary>
    public static string PlayablePath(MediaItem media)
    {
        if (media.Zoom > MaxZoomForCopy)
            return media.Path;

        try
        {
            var copy = CopyPathFor(media.Path);
            return copy is not null && File.Exists(copy) ? copy : media.Path;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo buscar la copia liviana de {Path}", media.Path);
            return media.Path;
        }
    }

    /// <summary>
    /// Checks every given video and makes the copies that are missing, one at
    /// a time in the background. Copies (and "no hace falta" notes) whose
    /// original is no longer in the list are deleted. Safe to call again
    /// whenever the library changes: what is already done is skipped.
    /// </summary>
    /// <param name="isBusy">
    /// True while a new copy must not start — the output is on, so the
    /// conversion would compete with the service for the processor.
    /// </param>
    public static void Refresh(IEnumerable<string> videoPaths, Func<bool> isBusy)
    {
        _isBusy = isBusy;

        var paths = videoPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (YtDlp.FindFfmpeg() is null || YtDlp.FindFfprobe() is null)
        {
            Log.Information("Sin ffmpeg/ffprobe: los videos se reproducen siempre desde el original");
            return;
        }

        try
        {
            Directory.CreateDirectory(Folder);
            RemoveOrphans(paths);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo preparar la carpeta de copias livianas");
            return;
        }

        lock (Sync)
        {
            foreach (var path in paths)
            {
                if (Queued.Add(path))
                    Pending.Enqueue(path);
            }

            if (_worker is null || _worker.IsCompleted)
                _worker = Task.Run(WorkAsync);
        }
    }

    // ── Cola ─────────────────────────────────────────────────────

    private static async Task WorkAsync()
    {
        while (true)
        {
            string path;
            lock (Sync)
            {
                if (Pending.Count == 0)
                    return;
                path = Pending.Dequeue();
            }

            try
            {
                // Never start a conversion in the middle of a service: wait
                // until the output is off. A copy already under way keeps
                // going at the lowest priority.
                while (_isBusy())
                    await Task.Delay(TimeSpan.FromSeconds(10));

                await ProcessAsync(path);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Falló la copia liviana de {Path}", path);
            }
            finally
            {
                lock (Sync)
                    Queued.Remove(path);
            }
        }
    }

    private static async Task ProcessAsync(string original)
    {
        if (!File.Exists(original) || CopyPathFor(original) is not { } copy)
            return;

        var skipMarker = Path.ChangeExtension(copy, ".ok");
        if (File.Exists(copy) || File.Exists(skipMarker))
            return;

        var info = await ProbeAsync(original);
        var plan = info is null ? null : LightCopyPolicy.Plan(info);
        if (plan is null)
        {
            // Light enough already (or unreadable): remember it, so the next
            // start doesn't open the file again.
            await File.WriteAllTextAsync(skipMarker, string.Empty);
            return;
        }

        var name = Path.GetFileNameWithoutExtension(original);
        StatusChanged?.Invoke($"Preparando copia liviana de «{name}»…");
        Log.Information("Copia liviana de {Path}: {W}×{H} a {Fps:0.##} fps → achicar {Shrink}, cuadros {NewFps}",
            original, info!.Width, info.Height, info.FrameRate, plan.Shrink,
            plan.ChangesFrameRate ? $"{plan.FrameRateNumerator}/{plan.FrameRateDenominator}" : "iguales");

        var partial = Path.ChangeExtension(copy, ".part.mp4");
        var stopwatch = Stopwatch.StartNew();
        _converting = partial;
        try
        {
            if (await EncodeAsync(original, partial, plan))
            {
                File.Move(partial, copy, overwrite: true);
                Log.Information("Copia liviana lista en {Seconds:0} s: {Copy}", stopwatch.Elapsed.TotalSeconds, copy);
                StatusChanged?.Invoke($"Copia liviana lista: «{name}». Se usa desde la próxima vez que lo proyectes.");
            }
            else
            {
                TryDelete(partial);
                StatusChanged?.Invoke($"No se pudo preparar la copia liviana de «{name}»; se sigue usando el original.");
            }
        }
        finally
        {
            _converting = null;
        }
    }

    // ── ffprobe / ffmpeg ─────────────────────────────────────────

    private static async Task<VideoStreamInfo?> ProbeAsync(string path)
    {
        var (exit, output) = await RunAsync(YtDlp.FindFfprobe()!,
        [
            "-v", "error", "-select_streams", "v:0",
            "-show_entries", "stream=width,height,avg_frame_rate,r_frame_rate",
            "-of", "csv=p=0", path,
        ], lowPriority: false);

        // "1920,1080,30/1,30/1" — the order ffprobe uses is its own, not the
        // order asked for, so the fields are told apart by shape.
        var line = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (exit != 0 || line is null)
            return null;

        var fields = line.Split(',');
        var numbers = fields.Where(f => !f.Contains('/')).Select(f => int.TryParse(f, out var n) ? n : 0).ToList();
        var rates = fields.Where(f => f.Contains('/')).Select(ParseRate).Where(r => r.Den > 0 && r.Num > 0).ToList();
        if (numbers.Count < 2 || rates.Count == 0)
            return null;

        // r_frame_rate can report a field rate (twice the real one) on
        // interlaced files; avg_frame_rate is the honest one when present.
        var rate = rates.MinBy(r => r.Num / (double)r.Den);
        return new VideoStreamInfo(numbers[0], numbers[1], rate.Num, rate.Den);
    }

    private static (int Num, int Den) ParseRate(string text)
    {
        var parts = text.Split('/');
        return parts.Length == 2
               && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var num)
               && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var den)
            ? (num, den)
            : (0, 0);
    }

    private static async Task<bool> EncodeAsync(string original, string target, LightCopyPlan plan)
    {
        var filters = new List<string>();
        if (plan.Shrink)
        {
            // Covers the 1920×1080 screen exactly like the original would,
            // and is worked out on the picture as shown — after a phone
            // video's rotation — so nothing is squashed.
            const string scale = "min(1,max(1920/iw,1080/ih))";
            filters.Add($"scale=w='trunc(iw*{scale}/2)*2':h='trunc(ih*{scale}/2)*2':flags=lanczos");
        }
        if (plan.ChangesFrameRate)
            filters.Add($"fps={plan.FrameRateNumerator}/{plan.FrameRateDenominator}");

        var arguments = new List<string>
        {
            "-hide_banner", "-nostdin", "-y", "-loglevel", "error",
            "-i", original,
            "-map", "0:v:0", "-map", "0:a:0?",
            "-vf", string.Join(',', filters),
            // H.264 is the format every graphics card of the last fifteen
            // years decodes. CRF 18 is visually lossless: on the church's
            // loops the copy measured 0.996 SSIM against the original.
            "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p",
            // Half the cores: the copy is not urgent, the computer may be in use.
            "-threads", Math.Max(1, Environment.ProcessorCount / 2).ToString(CultureInfo.InvariantCulture),
            "-c:a", "aac", "-b:a", "192k",
            "-movflags", "+faststart",
            target,
        };

        var (exit, output) = await RunAsync(YtDlp.FindFfmpeg()!, arguments, lowPriority: true);
        if (exit != 0)
            Log.Warning("ffmpeg terminó con {Exit}: {Output}", exit, output.Trim());
        return exit == 0 && File.Exists(target);
    }

    private static async Task<(int Exit, string Output)> RunAsync(
        string executable, IEnumerable<string> arguments, bool lowPriority)
    {
        var info = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = info };
        process.Start();

        if (lowPriority)
        {
            // Idle: it only gets the processor when nothing else wants it,
            // EcclesiaCast's own video included.
            try { process.PriorityClass = ProcessPriorityClass.Idle; } catch { /* ya terminó */ }
            _encoder = process;
        }

        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, await stdout + await stderr);
        }
        finally
        {
            if (lowPriority)
                _encoder = null;
        }
    }

    // ── Nombres y limpieza ───────────────────────────────────────

    /// <summary>Where the copy of this original lives (or would live), or null when it is not a file.</summary>
    private static string? CopyPathFor(string original)
    {
        var file = new FileInfo(original);
        if (!file.Exists)
            return null;

        var identity = $"{file.FullName.ToUpperInvariant()}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..20];
        return Path.Combine(Folder, hash + ".mp4");
    }

    private static void RemoveOrphans(IReadOnlyCollection<string> paths)
    {
        var keep = paths
            .Select(CopyPathFor)
            .Where(p => p is not null)
            .Select(p => Path.GetFileNameWithoutExtension(p!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(Folder))
        {
            if (string.Equals(file, _converting, StringComparison.OrdinalIgnoreCase))
                continue;

            var name = Path.GetFileName(file);
            var key = name.Split('.')[0];

            // A ".part" left by a conversion cut short (the program closed)
            // is never valid; the rest goes when its original is gone or changed.
            if (name.Contains(".part.", StringComparison.OrdinalIgnoreCase) || !keep.Contains(key))
                TryDelete(file);
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) { Log.Debug(ex, "No se pudo borrar {Path}", path); }
    }
}
