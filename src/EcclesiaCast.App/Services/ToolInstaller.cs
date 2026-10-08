using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// A download that failed for a reason the operator can understand (no
/// internet, GitHub unreachable). The message is already in Spanish.
/// </summary>
public sealed class ToolDownloadException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>
/// Fetches the helper programs EcclesiaCast manages for itself (yt-dlp and the
/// JavaScript runtime it needs for YouTube), so the operator never has to
/// install anything by hand. The addresses are properties so a test can point
/// them at a local server instead of GitHub.
/// </summary>
public sealed class ToolInstaller(string folder)
{
    /// <summary>The official yt-dlp release; "latest/download" always serves the newest.</summary>
    public string YtDlpUrl { get; init; } =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

    /// <summary>
    /// Deno is the runtime yt-dlp uses by default to solve YouTube's
    /// JavaScript challenges. It is a single executable inside a zip.
    /// </summary>
    public string DenoUrl { get; init; } =
        "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";

    public string YtDlpPath => Path.Combine(folder, "yt-dlp.exe");

    public string DenoPath => Path.Combine(folder, "deno.exe");

    public async Task DownloadYtDlpAsync(
        IProgress<(double Percent, string Message)> progress, CancellationToken cancellation)
    {
        Directory.CreateDirectory(folder);
        var partial = YtDlpPath + ".part";
        await DownloadAsync(YtDlpUrl, partial, "Bajando el descargador de videos", progress, cancellation);
        File.Move(partial, YtDlpPath, overwrite: true);
        Log.Information("yt-dlp instalado en {Path}", YtDlpPath);
    }

    public async Task DownloadDenoAsync(
        IProgress<(double Percent, string Message)> progress, CancellationToken cancellation)
    {
        Directory.CreateDirectory(folder);
        var zip = Path.Combine(folder, "deno.zip.part");
        try
        {
            await DownloadAsync(DenoUrl, zip, "Bajando un componente de YouTube", progress, cancellation);
            progress.Report((-1, "Preparando el componente de YouTube…"));

            using var archive = ZipFile.OpenRead(zip);
            var entry = archive.GetEntry("deno.exe")
                ?? throw new ToolDownloadException("El componente de YouTube bajado no es el esperado.");
            var partial = DenoPath + ".part";
            entry.ExtractToFile(partial, overwrite: true);
            File.Move(partial, DenoPath, overwrite: true);
            Log.Information("deno instalado en {Path}", DenoPath);
        }
        catch (InvalidDataException ex)
        {
            throw new ToolDownloadException("El componente de YouTube bajado está dañado.", ex);
        }
        finally
        {
            try { File.Delete(zip); } catch (IOException) { /* ya se limpiará */ }
        }
    }

    private static async Task DownloadAsync(
        string url,
        string destination,
        string label,
        IProgress<(double Percent, string Message)> progress,
        CancellationToken cancellation)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EcclesiaCast");

            using var response = await client.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead, cancellation);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength;
            await using var source = await response.Content.ReadAsStreamAsync(cancellation);
            await using (var target = File.Create(destination))
            {
                var buffer = new byte[81920];
                long read = 0;
                var lastReported = -1;
                int count;
                while ((count = await source.ReadAsync(buffer, cancellation)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, count), cancellation);
                    read += count;

                    if (total is > 0)
                    {
                        var percent = (int)(read * 100 / total.Value);
                        if (percent != lastReported)
                        {
                            lastReported = percent;
                            progress.Report((percent, $"{label}… {percent}%"));
                        }
                    }
                    else if (lastReported < 0)
                    {
                        lastReported = 0;
                        progress.Report((-1, $"{label}…"));
                    }
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException
                                   && !cancellation.IsCancellationRequested)
        {
            Log.Warning(ex, "No se pudo bajar {Url}", url);
            try { File.Delete(destination); } catch (IOException) { /* ya se limpiará */ }
            throw new ToolDownloadException(
                "No pude bajar el programa que descarga los videos. "
                + "Revisá que la computadora tenga internet y probá de nuevo.", ex);
        }
    }
}
