using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Downloads a YouTube video to a local file through yt-dlp, so a service
/// doesn't depend on the church's internet holding up. yt-dlp is not shipped
/// with EcclesiaCast: it is looked up on the system and, if missing, the
/// operator is told where to get it.
/// </summary>
public static partial class YtDlp
{
    /// <summary>Where a manually installed copy is looked for first.</summary>
    public static string ToolsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "tools");

    /// <summary>Default home for downloaded videos.</summary>
    public static string DownloadsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "EcclesiaCast");

    /// <summary>Full path to yt-dlp, or null when it isn't installed.</summary>
    public static string? FindExecutable() => Find("yt-dlp.exe");

    /// <summary>
    /// True when ffmpeg is around. Without it yt-dlp can't join the separate
    /// video and audio streams YouTube serves above 720p, so the download
    /// falls back to a single lower-resolution file.
    /// </summary>
    public static bool HasFfmpeg() => Find("ffmpeg.exe") is not null;

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
        var executable = FindExecutable()
            ?? throw new InvalidOperationException("yt-dlp no está instalado.");

        Directory.CreateDirectory(targetFolder);

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
        arguments.Add($"https://www.youtube.com/watch?v={videoId}");

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
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var percent))
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

        return downloaded;
    }
}
