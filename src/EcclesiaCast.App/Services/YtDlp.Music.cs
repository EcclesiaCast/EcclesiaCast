using System.Diagnostics;
using System.IO;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The YouTube music of the Música tab: search without a Google key, and the
/// sound alone of a video brought to this computer to play it with no
/// picture and no ads.
/// </summary>
/// <remarks>
/// The address of a sound that yt-dlp prints is refused (403) to any other
/// program that asks for it, the audio player included; yt-dlp's own
/// download does get it, and fast (a song in two or three seconds). So
/// «escuchar» brings the sound to a cache first and plays the file: the next
/// time it is instant and works without internet.
/// </remarks>
public static partial class YtDlp
{
    /// <summary>The m4a every YouTube video has (140), or the best sound there is.</summary>
    private const string AudioFormat = "140/bestaudio[ext=m4a]/bestaudio/best";

    /// <summary>The cache never grows past this; the songs not heard for the longest go first.</summary>
    private const long AudioCacheLimitBytes = 3L * 1024 * 1024 * 1024;

    /// <summary>Where the sound of YouTube videos is saved for good: next to the person's music.</summary>
    public static string MusicFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EcclesiaCast");

    /// <summary>Sounds brought to be heard: not the person's to look after, the program trims it.</summary>
    public static string AudioCacheFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcclesiaCast", "audio-youtube");

    /// <summary>A search and a song starting together must not both fetch the tools.</summary>
    private static readonly SemaphoreSlim ToolsGate = new(1, 1);

    /// <summary>Sounds on their way, so playing a song and preparing it ahead share one download.</summary>
    private static readonly Dictionary<string, Task<string>> AudioOnTheWay = [];

    private static async Task EnsureToolsQuietlyAsync(
        IProgress<(double Percent, string Message)> progress, CancellationToken cancellation)
    {
        await ToolsGate.WaitAsync(cancellation);
        try
        {
            await EnsureToolsAsync(progress, cancellation);
        }
        finally
        {
            ToolsGate.Release();
        }
    }

    /// <summary>
    /// Searches YouTube the way its own search box does and returns the first
    /// <paramref name="count"/> videos. Takes a few seconds.
    /// </summary>
    public static async Task<IReadOnlyList<YouTubeSearchResult>> SearchAsync(
        string query,
        int count,
        IProgress<(double Percent, string Message)> progress,
        CancellationToken cancellation)
    {
        await EnsureToolsQuietlyAsync(progress, cancellation);
        progress.Report((-1, $"Buscando «{query}» en YouTube…"));

        // «ytsearch» takes everything after the colon as the search, so the
        // text needs no escaping; a line break would end the argument though.
        var clean = query.Replace('\r', ' ').Replace('\n', ' ').Trim();
        var lines = await CaptureAsync(
            [
                "--flat-playlist", "--no-warnings", "--ignore-config",
                "--print", YouTubeAudio.SearchPrintTemplate,
                $"ytsearch{Math.Clamp(count, 1, 50)}:{clean}",
            ],
            TimeSpan.FromSeconds(45),
            cancellation);

        return lines
            .Select(YouTubeAudio.ParseSearchLine)
            .OfType<YouTubeSearchResult>()
            .DistinctBy(r => r.VideoId)
            .ToList();
    }

    /// <summary>
    /// The title, channel and length of one video (a link pasted in the
    /// search). If YouTube does not answer, the video still comes back, named
    /// by its id: its sound may be in the cache and play without internet.
    /// </summary>
    public static async Task<YouTubeSearchResult> LookUpAsync(
        string videoId,
        IProgress<(double Percent, string Message)> progress,
        CancellationToken cancellation)
    {
        var bare = new YouTubeSearchResult(videoId, $"YouTube {videoId}", string.Empty, null);
        try
        {
            await EnsureToolsQuietlyAsync(progress, cancellation);
            progress.Report((-1, "Buscando el video en YouTube…"));

            var arguments = new List<string>
            {
                "--no-playlist", "--no-warnings", "--ignore-config", "--ignore-no-formats-error",
                "--print", YouTubeAudio.SearchPrintTemplate,
            };
            if (FindRuntime() is { } runtime)
            {
                arguments.Add("--js-runtimes");
                arguments.Add($"deno:{runtime}");
            }

            arguments.Add(YouTubeUrl.WatchUrl(videoId));

            var lines = await CaptureAsync(arguments, TimeSpan.FromSeconds(45), cancellation);
            return lines.Select(YouTubeAudio.ParseSearchLine).OfType<YouTubeSearchResult>()
                       .FirstOrDefault(r => r.VideoId == videoId)
                   ?? bare;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warning(ex, "No se pudo leer el título del video {Id}", videoId);
            return bare;
        }
    }

    /// <summary>The sound of this video already in the cache, or null.</summary>
    public static string? CachedAudio(string videoId)
    {
        if (!Directory.Exists(AudioCacheFolder))
            return null;

        return new DirectoryInfo(AudioCacheFolder)
            .EnumerateFiles($"*[{videoId}].*")
            .Where(f => !f.Name.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
                        && !f.Name.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase)
                        && f.Length > 0)
            .Select(f => f.FullName)
            .FirstOrDefault();
    }

    /// <summary>
    /// The sound of a video as a file on this computer, ready to play: from
    /// the cache, or brought from YouTube now (with progress). Must be called
    /// on the UI thread: a video that asks for an account takes the session
    /// of the program's browser, which lives there.
    /// </summary>
    public static Task<string> GetAudioFileAsync(
        string videoId, IProgress<(double Percent, string Message)> progress)
    {
        if (CachedAudio(videoId) is { } cached)
        {
            Touch(cached);
            return Task.FromResult(cached);
        }

        lock (AudioOnTheWay)
        {
            if (AudioOnTheWay.TryGetValue(videoId, out var running))
                return running;

            var task = FetchAudioAsync(videoId, progress);
            AudioOnTheWay[videoId] = task;
            return task;
        }
    }

    private static async Task<string> FetchAudioAsync(
        string videoId, IProgress<(double Percent, string Message)> progress)
    {
        try
        {
            await EnsureToolsQuietlyAsync(progress, CancellationToken.None);

            string? file;
            try
            {
                // Without the session first: it needs the program's browser, and
                // almost no music asks for it.
                file = await DownloadAsync(videoId, AudioCacheFolder, progress, CancellationToken.None,
                    audioOnly: true, withSession: false);
            }
            catch (InvalidOperationException ex)
                when (DownloadFailure.Classify(ex.Message) is DownloadFailureKind.SignInRequired)
            {
                file = await DownloadAsync(videoId, AudioCacheFolder, progress, CancellationToken.None,
                    audioOnly: true, withSession: true);
            }

            if (file is null || !File.Exists(file))
                throw new InvalidOperationException("YouTube no devolvió el audio de este video.");

            // yt-dlp dates the file as YouTube does (the upload): it is dated now,
            // or the trimming would take the newest song first.
            Touch(file);
            TrimAudioCache(keep: file);
            return file;
        }
        finally
        {
            lock (AudioOnTheWay)
                AudioOnTheWay.Remove(videoId);
        }
    }

    /// <summary>A cached sound that failed to play is thrown away, so the next try brings it again.</summary>
    public static void ForgetCachedAudio(string videoId)
    {
        if (CachedAudio(videoId) is not { } file)
            return;

        try { File.Delete(file); } catch (IOException) { /* en uso: queda para la próxima limpieza */ }
        catch (UnauthorizedAccessException) { /* idem */ }
    }

    /// <summary>
    /// The sound of a video kept for good in the person's music folder, out of
    /// the cache (copied, or brought now). Returns the file.
    /// </summary>
    public static async Task<string> SaveAudioAsync(
        string videoId, IProgress<(double Percent, string Message)> progress)
    {
        var source = await GetAudioFileAsync(videoId, progress);

        Directory.CreateDirectory(MusicFolder);
        var target = Path.Combine(MusicFolder, Path.GetFileName(source));
        if (!File.Exists(target))
            File.Copy(source, target);
        return target;
    }

    /// <summary>Marks a cached sound as just heard, so the trimming keeps it.</summary>
    private static void Touch(string file)
    {
        try { File.SetLastWriteTimeUtc(file, DateTime.UtcNow); } catch (IOException) { /* no importa */ }
        catch (UnauthorizedAccessException) { /* no importa */ }
    }

    /// <summary>Takes the oldest sounds out of the cache while it is over its limit.</summary>
    private static void TrimAudioCache(string keep)
    {
        try
        {
            var files = new DirectoryInfo(AudioCacheFolder).EnumerateFiles()
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();

            long total = 0;
            foreach (var file in files)
            {
                total += file.Length;
                if (total <= AudioCacheLimitBytes
                    || string.Equals(file.FullName, keep, StringComparison.OrdinalIgnoreCase))
                    continue;

                try { file.Delete(); } catch (IOException) { /* sonando ahora: la próxima */ }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Debug(ex, "No se pudo achicar el caché de audios de YouTube");
        }
    }

    /// <summary>
    /// Runs yt-dlp and returns what it printed, line by line. A failure is an
    /// <see cref="InvalidOperationException"/> with its last error lines, the
    /// same text <see cref="DownloadFailure.Classify"/> reads.
    /// </summary>
    private static async Task<IReadOnlyList<string>> CaptureAsync(
        IReadOnlyList<string> arguments, TimeSpan limit, CancellationToken cancellation)
    {
        var executable = FindExecutable()
            ?? throw new InvalidOperationException("yt-dlp no está instalado.");

        var info = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        info.Environment["PYTHONUTF8"] = "1";
        // Not enough for the yt-dlp.exe build: on Windows it still printed titles in the
        // old code page («Hillsong en Espa?ol», 19/11). Asked outright, it writes UTF-8.
        info.ArgumentList.Add("--encoding");
        info.ArgumentList.Add("utf-8");
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = info };
        process.Start();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(limit);

        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errors = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { /* ya terminó */ }
            if (cancellation.IsCancellationRequested)
                throw;
            throw new InvalidOperationException("YouTube tardó demasiado en responder (timed out).");
        }

        var printed = await output;
        var failed = await errors;
        if (process.ExitCode != 0)
        {
            var detail = string.Join("\n", failed.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .TakeLast(4));
            Log.Warning("yt-dlp terminó con código {Code}: {Detail}", process.ExitCode, detail);
            throw new InvalidOperationException(detail.Length > 0 ? detail : "sin detalle");
        }

        return printed.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
    }

    /// <summary>A sentence for the operator when the YouTube music could not do its part.</summary>
    public static string ExplainMusic(Exception exception)
    {
        if (exception is ToolDownloadException)
            return exception.Message;

        return DownloadFailure.Classify(exception.Message) switch
        {
            DownloadFailureKind.Network => "No hay conexión a internet, o YouTube no respondió.",
            DownloadFailureKind.SignInRequired => "YouTube pide una cuenta para este video: probá con otro.",
            DownloadFailureKind.Unavailable => "Ese video es privado o ya no está disponible.",
            DownloadFailureKind.Forbidden or DownloadFailureKind.Challenge =>
                "YouTube no dejó sacar el audio ahora. Suele arreglarse solo en unos días; mientras tanto probá con otro video.",
            _ => "YouTube no respondió como se esperaba. Probá de nuevo en un rato.",
        };
    }
}
