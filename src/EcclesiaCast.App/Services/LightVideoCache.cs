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
/// re-exported original gets a fresh copy instead of a stale one. Next to it
/// live small empty marker files that remember what was found out about the
/// video: <c>.ok</c> (light enough, no copy needed), <c>.heavy</c> (heavy) and
/// <c>.no</c> (heavy, and the operator said no copy).
/// </summary>
public static class LightVideoCache
{
    /// <summary>
    /// Local, not roaming, and regenerable: a cache, not user data. A test
    /// build pointed at a copy of a library (ECCLESIACAST_DATA_DIR) keeps its
    /// copies there too: the clean-up deletes every copy its own library does
    /// not know, so one that looked in the real folder would empty it.
    /// </summary>
    public static string Folder =>
        Environment.GetEnvironmentVariable("ECCLESIACAST_DATA_DIR") is { Length: > 0 } dataDir
            ? Path.Combine(dataDir, "copias-livianas")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EcclesiaCast", "copias-livianas");

    /// <summary>
    /// A copy at 1080p looks like the original only while it is not zoomed
    /// in; past this the original is played so a close-up stays sharp.
    /// </summary>
    private const double MaxZoomForCopy = 1.05;

    private const string LightMarker = ".ok";
    private const string HeavyMarker = ".heavy";
    private const string DeclinedMarker = ".no";

    private static readonly object Sync = new();
    private static readonly Queue<string> Pending = new();
    private static readonly HashSet<string> Queued = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Videos whose copy the operator is being asked about right now: the
    /// background worker must leave them alone until the answer is in.
    /// </summary>
    private static readonly HashSet<string> Held = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>One conversion at a time, whether it is a background one or one asked for.</summary>
    private static readonly SemaphoreSlim EncodeGate = new(1, 1);

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

    // ── Lo que se sabe de un video ───────────────────────────────

    /// <summary>
    /// True for a video already found to be heavy (4K or 50+ frames), whether
    /// or not it has a copy. Only what the cache has looked at is known: a
    /// video it has not examined yet counts as light. The operator's LIVE panel
    /// asks this to show such a video as a still poster instead of drawing a
    /// second, moving copy of a picture the projector already struggles with.
    /// </summary>
    public static bool IsHeavy(MediaItem media) =>
        media.Type == MediaType.Video && IsHeavy(media.Path);

    public static bool IsHeavy(string path)
    {
        try
        {
            return CopyPathFor(path) is { } copy
                   && (File.Exists(copy) || File.Exists(Marker(copy, HeavyMarker)));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo ver si {Path} es pesado", path);
            return false;
        }
    }

    /// <summary>True when the light copy of this video is already made.</summary>
    public static bool HasCopy(string path)
    {
        try
        {
            return CopyPathFor(path) is { } copy && File.Exists(copy);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo buscar la copia liviana de {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// The operator does not want a copy of this video: it is remembered, so
    /// the background work does not make one behind their back.
    /// </summary>
    public static void Decline(string path)
    {
        try
        {
            if (CopyPathFor(path) is not { } copy)
                return;

            Directory.CreateDirectory(Folder);
            File.WriteAllText(Marker(copy, HeavyMarker), string.Empty);
            File.WriteAllText(Marker(copy, DeclinedMarker), string.Empty);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo anotar que {Path} se queda sin copia", path);
        }
    }

    /// <summary>Starts (or restarts) answering for a video: <see cref="Release"/> when done.</summary>
    public static void Hold(IEnumerable<string> paths)
    {
        lock (Sync)
            foreach (var path in paths)
                Held.Add(path);
    }

    public static void Release(IEnumerable<string> paths)
    {
        lock (Sync)
            foreach (var path in paths)
                Held.Remove(path);
    }

    private static bool IsHeld(string path)
    {
        lock (Sync)
            return Held.Contains(path);
    }

    /// <summary>Whether ffmpeg and ffprobe are there to look at a video and to make the copy.</summary>
    public static bool IsAvailable => YtDlp.FindFfmpeg() is not null && YtDlp.FindFfprobe() is not null;

    /// <summary>
    /// What the video's picture looks like, or null when it can't be read (no
    /// ffprobe, or not a video). Slow enough (a process start) to keep off the UI thread.
    /// </summary>
    public static async Task<VideoStreamInfo?> ProbeAsync(string path)
    {
        if (YtDlp.FindFfprobe() is not { } ffprobe)
            return null;

        var (exit, output) = await RunAsync(ffprobe,
        [
            "-v", "error", "-select_streams", "v:0",
            "-show_entries", "stream=width,height,avg_frame_rate,r_frame_rate",
            "-of", "csv=p=0", path,
        ], EncodePriority.Normal);

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

    private static async Task<TimeSpan> ProbeDurationAsync(string path)
    {
        if (YtDlp.FindFfprobe() is not { } ffprobe)
            return TimeSpan.Zero;

        var (exit, output) = await RunAsync(ffprobe,
            ["-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path],
            EncodePriority.Normal);

        return exit == 0 && FfmpegProgress.TryParseDuration(output, out var duration) ? duration : TimeSpan.Zero;
    }

    // ── Una copia pedida ahora ───────────────────────────────────

    /// <summary>
    /// Makes the copy right now, for a video the operator said yes to, and
    /// reports how far along it is (0 to 1). Returns false when it could not be
    /// made (the original keeps playing); throws <see cref="OperationCanceledException"/>
    /// when the operator cancels, after throwing away the half-written file.
    /// </summary>
    public static async Task<bool> MakeCopyNowAsync(
        string original, IProgress<double>? progress, CancellationToken cancellation)
    {
        if (!File.Exists(original) || CopyPathFor(original) is not { } copy || !IsAvailable)
            return false;

        if (File.Exists(copy))
            return true;

        var info = await ProbeAsync(original);
        var plan = info is null ? null : LightCopyPolicy.Plan(info);
        if (plan is null)
            return false;

        var total = await ProbeDurationAsync(original);
        Directory.CreateDirectory(Folder);
        await File.WriteAllTextAsync(Marker(copy, HeavyMarker), string.Empty, CancellationToken.None);

        // A background copy of another video may be running: wait for it.
        await EncodeGate.WaitAsync(cancellation);
        try
        {
            if (File.Exists(copy))
                return true;

            var ok = await BuildCopyAsync(original, copy, info!, plan, total, progress, EncodePriority.BelowNormal, cancellation);
            if (ok)
                TryDelete(Marker(copy, DeclinedMarker));
            return ok;
        }
        finally
        {
            EncodeGate.Release();
        }
    }

    // ── Copias en segundo plano ──────────────────────────────────

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

        if (!IsAvailable)
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
        // A video the operator is being asked about is not touched until the
        // answer is in; the next refresh finds it settled.
        if (!File.Exists(original) || CopyPathFor(original) is not { } copy || IsHeld(original))
            return;

        if (IsSettled(copy))
            return;

        // What the video is comes first, and does not wait for the output to
        // be off: it is a quick look, and it lets the panels know right away
        // that this one is heavy.
        var info = await ProbeAsync(original);
        var plan = info is null ? null : LightCopyPolicy.Plan(info);
        if (plan is null)
        {
            // Light enough already (or unreadable): remember it, so the next
            // start doesn't open the file again.
            await File.WriteAllTextAsync(Marker(copy, LightMarker), string.Empty);
            return;
        }

        await File.WriteAllTextAsync(Marker(copy, HeavyMarker), string.Empty);

        // Never start a conversion in the middle of a service: wait until the
        // output is off. A copy already under way keeps going at the lowest
        // priority.
        while (_isBusy())
            await Task.Delay(TimeSpan.FromSeconds(10));

        var total = await ProbeDurationAsync(original);
        var name = Path.GetFileNameWithoutExtension(original);

        await EncodeGate.WaitAsync();
        try
        {
            if (IsHeld(original) || IsSettled(copy))
                return;

            StatusChanged?.Invoke($"Preparando copia liviana de «{name}»…");
            if (await BuildCopyAsync(original, copy, info!, plan, total, progress: null, EncodePriority.Idle, CancellationToken.None))
                StatusChanged?.Invoke($"Copia liviana lista: «{name}». Se usa desde la próxima vez que lo proyectes.");
            else
                StatusChanged?.Invoke($"No se pudo preparar la copia liviana de «{name}»; se sigue usando el original.");
        }
        finally
        {
            EncodeGate.Release();
        }
    }

    /// <summary>Nothing left to do for this video: it has its copy, is light, or was declined.</summary>
    private static bool IsSettled(string copy) =>
        File.Exists(copy)
        || File.Exists(Marker(copy, LightMarker))
        || File.Exists(Marker(copy, DeclinedMarker));

    /// <summary>
    /// Writes the copy to a temporary name and moves it into place only when
    /// it is whole, so a copy cut short is never mistaken for a good one.
    /// Call with the <see cref="EncodeGate"/> held.
    /// </summary>
    private static async Task<bool> BuildCopyAsync(
        string original, string copy, VideoStreamInfo info, LightCopyPlan plan, TimeSpan total,
        IProgress<double>? progress, EncodePriority priority, CancellationToken cancellation)
    {
        Log.Information("Copia liviana de {Path}: {W}×{H} a {Fps:0.##} fps → achicar {Shrink}, cuadros {NewFps}",
            original, info.Width, info.Height, info.FrameRate, plan.Shrink,
            plan.ChangesFrameRate ? $"{plan.FrameRateNumerator}/{plan.FrameRateDenominator}" : "iguales");

        var partial = Path.ChangeExtension(copy, ".part.mp4");
        var stopwatch = Stopwatch.StartNew();
        _converting = partial;
        try
        {
            if (await EncodeAsync(original, partial, plan, total, progress, priority, cancellation))
            {
                File.Move(partial, copy, overwrite: true);
                await File.WriteAllTextAsync(Marker(copy, HeavyMarker), string.Empty, CancellationToken.None);
                Log.Information("Copia liviana lista en {Seconds:0} s: {Copy}", stopwatch.Elapsed.TotalSeconds, copy);
                return true;
            }

            TryDelete(partial);
            return false;
        }
        catch (OperationCanceledException)
        {
            TryDelete(partial);
            throw;
        }
        finally
        {
            _converting = null;
        }
    }

    // ── ffprobe / ffmpeg ─────────────────────────────────────────

    private static (int Num, int Den) ParseRate(string text)
    {
        var parts = text.Split('/');
        return parts.Length == 2
               && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var num)
               && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var den)
            ? (num, den)
            : (0, 0);
    }

    private static async Task<bool> EncodeAsync(
        string original, string target, LightCopyPlan plan, TimeSpan total,
        IProgress<double>? progress, EncodePriority priority, CancellationToken cancellation)
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

        // In the background the copy is not urgent and the computer may be in
        // use: half the cores. Asked for, the operator is waiting for it.
        var threads = priority == EncodePriority.Idle
            ? Math.Max(1, Environment.ProcessorCount / 2)
            : Math.Max(1, Environment.ProcessorCount - 1);

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
            "-threads", threads.ToString(CultureInfo.InvariantCulture),
            "-c:a", "aac", "-b:a", "192k",
            "-movflags", "+faststart",
        };

        // ffmpeg reports how much it has done on stdout, a block of lines
        // every half second, which is what the progress bar follows.
        if (progress is not null)
            arguments.AddRange(["-progress", "pipe:1", "-nostats"]);
        arguments.Add(target);

        Action<string>? onLine = progress is null
            ? null
            : line =>
            {
                if (FfmpegProgress.TryParseTime(line, out var done))
                    progress.Report(FfmpegProgress.Fraction(done, total));
            };

        var (exit, output) = await RunAsync(YtDlp.FindFfmpeg()!, arguments, priority, onLine, cancellation);
        if (exit != 0)
            Log.Warning("ffmpeg terminó con {Exit}: {Output}", exit, output.Trim());
        return exit == 0 && File.Exists(target);
    }

    private enum EncodePriority { Normal, BelowNormal, Idle }

    private static async Task<(int Exit, string Output)> RunAsync(
        string executable, IEnumerable<string> arguments, EncodePriority priority,
        Action<string>? onOutputLine = null, CancellationToken cancellation = default)
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
        var stdout = new StringBuilder();
        if (onOutputLine is not null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null)
                    return;

                lock (stdout)
                    stdout.AppendLine(e.Data);
                onOutputLine(e.Data);
            };
        }

        process.Start();

        // Read whole when nobody follows it line by line: ffprobe's one short
        // line must never be cut off by the process ending before the last
        // event arrives.
        var wholeOutput = onOutputLine is null ? process.StandardOutput.ReadToEndAsync() : null;
        if (onOutputLine is not null)
            process.BeginOutputReadLine();

        if (priority != EncodePriority.Normal)
        {
            // Idle only gets the processor when nothing else wants it, EcclesiaCast's
            // own video included; below normal still gives way to it.
            try
            {
                process.PriorityClass = priority == EncodePriority.Idle
                    ? ProcessPriorityClass.Idle
                    : ProcessPriorityClass.BelowNormal;
            }
            catch { /* ya terminó */ }
            _encoder = process;
        }

        // Cancelling kills ffmpeg (and anything it started): the half-written
        // file is thrown away by whoever asked.
        using var registration = cancellation.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ya terminó */ }
        });

        try
        {
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(CancellationToken.None);
            var errors = await stderr;
            cancellation.ThrowIfCancellationRequested();

            string text;
            if (wholeOutput is not null)
            {
                text = await wholeOutput;
            }
            else
            {
                // Lets the line events that are still on their way arrive.
                process.WaitForExit();
                lock (stdout)
                    text = stdout.ToString();
            }

            return (process.ExitCode, text + errors);
        }
        finally
        {
            if (priority != EncodePriority.Normal)
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

    /// <summary>A marker file kept beside a copy: the same name with another ending.</summary>
    private static string Marker(string copy, string extension) => Path.ChangeExtension(copy, extension);

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
