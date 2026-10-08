using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Downloads a YouTube video to a local file through yt-dlp, so a service
/// doesn't depend on the church's internet holding up. EcclesiaCast looks after
/// yt-dlp by itself: it fetches the official build when it is missing, keeps it
/// up to date (YouTube changes every few weeks and an old copy gets 403) and
/// hands it the YouTube session, so the operator never types a command.
/// </summary>
public static partial class YtDlp
{
    /// <summary>Where the helper programs live; yt-dlp finds a runtime placed next to it.</summary>
    public static string ToolsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "tools");

    /// <summary>Default home for downloaded videos.</summary>
    public static string DownloadsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "EcclesiaCast");

    private static readonly ToolInstaller Installer = new(ToolsFolder);

    /// <summary>
    /// Full path to the yt-dlp EcclesiaCast manages, or null when it isn't there
    /// yet. A copy found on the PATH is ignored on purpose: a package manager's
    /// copy can't update itself with <c>-U</c>.
    /// </summary>
    public static string? FindExecutable() =>
        File.Exists(Installer.YtDlpPath) ? Installer.YtDlpPath : null;

    /// <summary>The JavaScript runtime YouTube needs, or null when it isn't around.</summary>
    private static string? FindRuntime() =>
        File.Exists(Installer.DenoPath) ? Installer.DenoPath : Find("deno.exe");

    /// <summary>
    /// True when ffmpeg is around. Without it yt-dlp can't join the separate
    /// video and audio streams YouTube serves above 720p, so the download
    /// falls back to a single lower-resolution file.
    /// </summary>
    public static bool HasFfmpeg() => Find("ffmpeg.exe") is not null;

    /// <summary>Full path to ffmpeg, or null; the light video copies use it too.</summary>
    public static string? FindFfmpeg() => Find("ffmpeg.exe");

    /// <summary>Full path to ffprobe, which ships next to ffmpeg; null when missing.</summary>
    public static string? FindFfprobe() => Find("ffprobe.exe");

    private static string? Find(string fileName)
    {
        var local = Path.Combine(ToolsFolder, fileName);
        if (File.Exists(local))
            return local;

        foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                 .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(folder.Trim('"'), fileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch (ArgumentException)
            {
                // Una entrada inválida del PATH no debe romper la búsqueda.
            }
        }

        return null;
    }

    /// <summary>Percentage lines yt-dlp prints while downloading, e.g. "[download]  42.7% of ...".</summary>
    [GeneratedRegex(@"\[download\]\s+(\d{1,3}(?:[.,]\d+)?)%")]
    private static partial Regex ProgressLine();

    // ── Keeping the tools ready ──────────────────────────────────

    private static string CheckStampPath => Path.Combine(ToolsFolder, "yt-dlp.checked");

    private static DateTimeOffset? _lastUpdateAttempt;

    private static DateTimeOffset? ReadCheckStamp()
    {
        try
        {
            return DateTimeOffset.TryParse(File.ReadAllText(CheckStampPath), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var stamp) ? stamp : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void WriteCheckStamp()
    {
        try
        {
            Directory.CreateDirectory(ToolsFolder);
            File.WriteAllText(CheckStampPath, DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        }
        catch (IOException ex)
        {
            Log.Warning(ex, "No se pudo anotar la fecha de la última revisión de yt-dlp");
        }
    }

    /// <summary>
    /// Makes sure yt-dlp and its JavaScript runtime exist and that yt-dlp is not
    /// older than three days since its last check. Downloads what is missing,
    /// reporting progress; an update that can't be done (no internet at that
    /// moment) is not an error, the download is tried with what there is.
    /// </summary>
    public static async Task EnsureToolsAsync(
        IProgress<(double Percent, string Message)> progress, CancellationToken cancellation)
    {
        if (FindExecutable() is null)
        {
            await Installer.DownloadYtDlpAsync(progress, cancellation);
            WriteCheckStamp(); // a fresh download is the newest by definition
        }

        if (FindRuntime() is null)
        {
            try
            {
                await Installer.DownloadDenoAsync(progress, cancellation);
            }
            catch (ToolDownloadException ex)
            {
                // Some videos still work without it; let yt-dlp try.
                Log.Warning(ex, "No se pudo bajar el componente de YouTube");
            }
        }

        if (ToolUpdatePolicy.IsDue(ReadCheckStamp(), DateTimeOffset.UtcNow))
            await UpdateAsync(progress, cancellation);
    }

    /// <summary>Asks yt-dlp to update itself. Returns false when it couldn't (offline, blocked).</summary>
    private static async Task<bool> UpdateAsync(
        IProgress<(double Percent, string Message)> progress, CancellationToken cancellation)
    {
        var executable = FindExecutable();
        if (executable is null)
            return false;

        // One update per minute is plenty: the 403 retry and the three-day check
        // can land together.
        if (_lastUpdateAttempt is { } last && DateTimeOffset.UtcNow - last < TimeSpan.FromMinutes(1))
            return true;
        _lastUpdateAttempt = DateTimeOffset.UtcNow;

        progress.Report((-1, "Buscando una versión nueva del descargador…"));
        try
        {
            var info = new ProcessStartInfo(executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            info.ArgumentList.Add("-U");

            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync(cancellation);
            var errors = process.StandardError.ReadToEndAsync(cancellation);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ya terminó */ }
                Log.Warning("yt-dlp -U tardó demasiado y se cortó");
                return false;
            }

            Log.Information("yt-dlp -U ({Code}): {Output} {Errors}",
                process.ExitCode, (await output).Trim(), (await errors).Trim());
            if (process.ExitCode != 0)
                return false;

            WriteCheckStamp();
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException
                                   or System.ComponentModel.Win32Exception)
        {
            Log.Warning(ex, "No se pudo actualizar yt-dlp");
            return false;
        }
    }

    // ── Telling the operator what happened ───────────────────────

    /// <summary>
    /// Turns what went wrong into a sentence for the operator: what happened,
    /// and that the video can still be projected over the internet. Nothing
    /// here asks anyone to open a terminal.
    /// </summary>
    public static string Explain(Exception exception)
    {
        if (exception is ToolDownloadException)
            return exception.Message + "\n\nMientras tanto, el video se puede proyectar por internet.";

        const string Fallback = "\n\nEl video se puede proyectar por internet igual: "
            + "queda en la biblioteca y se reproduce con conexión.";

        return DownloadFailure.Classify(exception.Message) switch
        {
            DownloadFailureKind.Network =>
                "No hay conexión a internet, o se cortó en medio de la descarga." + Fallback,
            DownloadFailureKind.SignInRequired =>
                "YouTube pide una cuenta para este video. Entrá con la cuenta de la iglesia "
                + "desde el botón «▶ YouTube» y volvé a intentar." + Fallback,
            DownloadFailureKind.Unavailable =>
                "El video es privado o no está disponible para esta cuenta." + Fallback,
            DownloadFailureKind.Forbidden or DownloadFailureKind.Challenge =>
                "YouTube no dejó descargar este video, aunque el programa ya buscó una "
                + "versión nueva del descargador. Suele arreglarse solo en unos días." + Fallback,
            _ =>
                "No se pudo descargar el video." + Fallback,
        };
    }

    // ── Downloading ──────────────────────────────────────────────

    /// <summary>
    /// Downloads one video and returns the file it produced. Reports progress
    /// as (percentage 0–100 or -1 when unknown, message).
    /// </summary>
    public static async Task<string?> DownloadAsync(
        string videoId,
        string targetFolder,
        IProgress<(double Percent, string Message)> progress,
        CancellationToken cancellation)
    {
        await EnsureToolsAsync(progress, cancellation);

        progress.Report((-1, "Preparando la descarga…"));
        var cookies = await YouTubeCookies.ExportAsync();
        try
        {
            try
            {
                return await RunAsync(videoId, targetFolder, cookies, progress, cancellation);
            }
            catch (InvalidOperationException ex)
                when (DownloadFailure.Classify(ex.Message) is DownloadFailureKind.Forbidden
                                                             or DownloadFailureKind.Challenge)
            {
                // The usual cause is a yt-dlp that is behind YouTube: update it and
                // try once more before giving up.
                Log.Warning("YouTube rechazó la descarga; se actualiza yt-dlp y se reintenta: {Error}", ex.Message);
                await UpdateAsync(progress, cancellation);
                progress.Report((-1, "Reintentando la descarga…"));
                return await RunAsync(videoId, targetFolder, cookies, progress, cancellation);
            }
        }
        finally
        {
            if (cookies is not null)
            {
                try { File.Delete(cookies); } catch (IOException) { /* es un temporal */ }
            }
        }
    }

    private static async Task<string?> RunAsync(
        string videoId,
        string targetFolder,
        string? cookieFile,
        IProgress<(double Percent, string Message)> progress,
        CancellationToken cancellation)
    {
        var executable = FindExecutable()
            ?? throw new InvalidOperationException("yt-dlp no está instalado.");

        Directory.CreateDirectory(targetFolder);
        var startedAt = DateTime.Now.AddSeconds(-2);

        // Above 720p YouTube serves video and audio apart, and joining them
        // needs ffmpeg; without it, ask for the best single file instead.
        var format = HasFfmpeg()
            ? "bestvideo[height<=?1080]+bestaudio/best[height<=?1080]/best"
            : "best[height<=?1080][ext=mp4]/best[ext=mp4]/best";

        var arguments = new List<string>
        {
            "--no-playlist", "--no-warnings", "--newline", "--no-simulate",
            "--print", "after_move:filepath",
            "-f", format,
            "-P", targetFolder,
            "-o", "%(title)s [%(id)s].%(ext)s",
        };
        if (HasFfmpeg())
        {
            arguments.Add("--merge-output-format");
            arguments.Add("mp4");
        }

        if (FindRuntime() is { } runtime)
        {
            // YouTube hides its stream addresses behind a small JavaScript puzzle
            // that yt-dlp solves with this runtime.
            arguments.Add("--js-runtimes");
            arguments.Add($"deno:{runtime}");
        }

        if (cookieFile is not null)
        {
            arguments.Add("--cookies");
            arguments.Add(cookieFile);
        }

        arguments.Add($"https://www.youtube.com/watch?v={videoId}");

        var info = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // Python would otherwise print titles in the console's old code page.
        info.Environment["PYTHONUTF8"] = "1";
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = info };

        // yt-dlp prints the resulting path last; keep the most recent line
        // that is a real file, since progress shares the same stream.
        string? downloaded = null;
        var errors = new List<string>();

        process.Start();

        var readErrors = Task.Run(async () =>
        {
            while (await process.StandardError.ReadLineAsync(cancellation) is { } line)
            {
                if (line.Trim().Length > 0)
                    errors.Add(line);
                Log.Debug("yt-dlp: {Line}", line);
            }
        }, cancellation);

        try
        {
            while (await process.StandardOutput.ReadLineAsync(cancellation) is { } line)
            {
                if (ProgressLine().Match(line) is { Success: true } match
                    && double.TryParse(match.Groups[1].Value.Replace(',', '.'),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
                {
                    progress.Report((percent, $"Descargando… {percent:0}%"));
                    continue;
                }

                if (line.StartsWith("[Merger]", StringComparison.Ordinal)
                    || line.StartsWith("[ExtractAudio]", StringComparison.Ordinal))
                {
                    progress.Report((-1, "Uniendo video y audio…"));
                    continue;
                }

                var candidate = line.Trim().Trim('"');
                if (candidate.Length > 0 && File.Exists(candidate))
                    downloaded = candidate;
            }

            await process.WaitForExitAsync(cancellation);
            await readErrors;
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw;
        }

        if (process.ExitCode != 0)
        {
            var detail = errors.Count > 0 ? string.Join("\n", errors.TakeLast(4)) : "sin detalle";
            Log.Error("yt-dlp terminó con código {Code}: {Detail}", process.ExitCode, detail);
            throw new InvalidOperationException(detail);
        }

        // If the printed path didn't survive the trip (odd characters in a
        // title), the file is still the newest one in the folder.
        return downloaded ?? NewestFileSince(targetFolder, startedAt);
    }

    /// <summary>The most recently written finished video in <paramref name="folder"/> since <paramref name="since"/>.</summary>
    private static string? NewestFileSince(string folder, DateTime since) =>
        new DirectoryInfo(folder).EnumerateFiles()
            .Where(f => f.LastWriteTime >= since
                        && !f.Name.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
                        && !f.Name.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTime)
            .Select(f => f.FullName)
            .FirstOrDefault();
}
