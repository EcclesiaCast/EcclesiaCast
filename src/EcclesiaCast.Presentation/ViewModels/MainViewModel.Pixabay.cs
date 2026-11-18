using System.IO;
using System.Net;
using System.Net.Http;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// «Buscar fondos en Pixabay»: videos and photos that may be used for free,
/// searched through our own service (it keeps the Pixabay key) and downloaded
/// straight from Pixabay into the open media tab.
/// </summary>
public sealed partial class MainViewModel
{
    private static readonly HttpClient PixabayHttp = CreatePixabayClient();

    /// <summary>Where the downloads are kept: the library's own copy, like the other backgrounds.</summary>
    private string PixabayFolder => Path.Combine(_paths.DataFolder, "fondos-pixabay");

    private static HttpClient CreatePixabayClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        return client;
    }

    /// <summary>Whether a file is already in the media library (for «Ya está en tus medios»).</summary>
    public bool HasMediaFile(string path) =>
        _allMedia.Any(m => string.Equals(m.Path, path, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// One page of results. Throws <see cref="PixabaySearchException"/> with a
    /// sentence for the operator when it can not search.
    /// </summary>
    public async Task<PixabayPage> SearchPixabayAsync(string query, bool photos, int page, CancellationToken cancel)
    {
        var url = $"{AppInfo.CurrentBackgroundSearchUrl}?q={Uri.EscapeDataString(query.Trim())}"
                  + $"&tipo={(photos ? "fotos" : "videos")}&pagina={page}";
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            using var response = await PixabayHttp.GetAsync(url, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.ServiceUnavailable && PixabayBackgrounds.ErrorOf(body) is null)
                throw new PixabaySearchException("La búsqueda de fondos todavía no está disponible.");
            if (!response.IsSuccessStatusCode)
                throw new PixabaySearchException(PixabayBackgrounds.ErrorOf(body) ?? "La búsqueda no respondió. Probá de nuevo en un rato.");

            return PixabayBackgrounds.ParsePage(body);
        }
        catch (FormatException ex)
        {
            Log.Warning(ex, "La búsqueda de Pixabay devolvió algo que no se pudo leer");
            throw new PixabaySearchException("La búsqueda no respondió bien. Probá de nuevo en un rato.");
        }
        catch (HttpRequestException ex)
        {
            Log.Information(ex, "No se pudo buscar en Pixabay");

            // The address of the service does not exist yet (it is not
            // published): that is not the operator's connection.
            if (ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.HostNotFound })
                throw new PixabaySearchException("La búsqueda de fondos todavía no está disponible.");
            throw new PixabaySearchException("No se pudo conectar con la búsqueda. Revisá la conexión a internet.");
        }
        catch (TaskCanceledException) when (!cancel.IsCancellationRequested)
        {
            throw new PixabaySearchException("La búsqueda tardó demasiado. Probá de nuevo.");
        }
    }

    /// <summary>
    /// Downloads the chosen results and brings them into the open tab, the
    /// same way as adding files by hand (thumbnails, light copies…). One that
    /// is already in the library is not downloaded twice. Returns how many
    /// were added and how many failed.
    /// </summary>
    public async Task<(int Added, int Failed)> AddPixabayAsync(
        IReadOnlyList<PixabayHit> hits, string query, IProgress<string> progress, CancellationToken cancel)
    {
        Directory.CreateDirectory(PixabayFolder);
        var ready = new List<string>();
        var failed = 0;

        for (var n = 0; n < hits.Count; n++)
        {
            var hit = hits[n];
            if (hit.File is not { } file || !PixabayBackgrounds.IsPixabayAddress(file.Url))
            {
                failed++;
                continue;
            }

            var path = Path.Combine(PixabayFolder, PixabayBackgrounds.FileName(hit, query));
            if (_allMedia.Any(m => string.Equals(m.Path, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                if (!File.Exists(path))
                    await DownloadPixabayFile(file.Url, path, n + 1, hits.Count, progress, cancel);
                ready.Add(path);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
                                           or TaskCanceledException && !cancel.IsCancellationRequested)
            {
                Log.Warning(ex, "No se pudo bajar {Url} de Pixabay", file.Url);
                failed++;
            }
        }

        cancel.ThrowIfCancellationRequested();
        if (ready.Count > 0)
        {
            NoteAction($"Agregar {ready.Count} fondo(s) de Pixabay a «{SelectedMediaTab}»");
            ImportMediaFiles(ready);
            Log.Information("Fondos de Pixabay agregados: {Count} (fallaron {Failed})", ready.Count, failed);
        }

        return (ready.Count, failed);
    }

    private static async Task DownloadPixabayFile(
        string url, string path, int number, int total, IProgress<string> progress, CancellationToken cancel)
    {
        var partial = path + ".bajando";
        try
        {
            using var response = await PixabayHttp.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel);
            response.EnsureSuccessStatusCode();
            var length = response.Content.Headers.ContentLength;

            await using (var source = await response.Content.ReadAsStreamAsync(cancel))
            await using (var target = File.Create(partial))
            {
                var buffer = new byte[81920];
                long read = 0;
                var lastPercent = -1;
                int count;
                while ((count = await source.ReadAsync(buffer, cancel)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, count), cancel);
                    read += count;
                    var percent = length is > 0 ? (int)(read * 100 / length.Value) : -1;
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        progress.Report(total == 1
                            ? $"Bajando… {Math.Max(percent, 0)}%"
                            : $"Bajando {number} de {total}… {Math.Max(percent, 0)}%");
                    }
                }
            }

            File.Move(partial, path, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(partial)) File.Delete(partial); } catch (IOException) { /* se limpia la próxima */ }
        }
    }
}

/// <summary>Why the search of backgrounds could not be done, in words for the operator.</summary>
public sealed class PixabaySearchException(string message) : Exception(message);
