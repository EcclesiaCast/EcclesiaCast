using System.IO;
using System.Net;
using System.Net.Http;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// «Buscar fondos en Pexels»: videos and photos that may be used for free,
/// searched through our own service (it keeps the Pexels key) and downloaded
/// straight from Pexels into the open media tab.
/// </summary>
public sealed partial class MainViewModel
{
    private static readonly HttpClient PexelsHttp = CreatePexelsClient();

    /// <summary>Where the downloads are kept: the library's own copy, like the other backgrounds.</summary>
    private static string PexelsFolder => Path.Combine(App.DataDir, "fondos-pexels");

    private static HttpClient CreatePexelsClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        return client;
    }

    /// <summary>Whether a file is already in the media library (for «Ya está en tus medios»).</summary>
    public bool HasMediaFile(string path) =>
        _allMedia.Any(m => string.Equals(m.Path, path, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// One page of results. Throws <see cref="PexelsSearchException"/> with a
    /// sentence for the operator when it can not search.
    /// </summary>
    public async Task<PexelsPage> SearchPexelsAsync(string query, bool photos, int page, CancellationToken cancel)
    {
        var url = $"{AppInfo.CurrentBackgroundSearchUrl}?q={Uri.EscapeDataString(query.Trim())}"
                  + $"&tipo={(photos ? "fotos" : "videos")}&pagina={page}";
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            using var response = await PexelsHttp.GetAsync(url, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.ServiceUnavailable && PexelsBackgrounds.ErrorOf(body) is null)
                throw new PexelsSearchException("La búsqueda de fondos todavía no está disponible.");
            if (!response.IsSuccessStatusCode)
                throw new PexelsSearchException(PexelsBackgrounds.ErrorOf(body) ?? "La búsqueda no respondió. Probá de nuevo en un rato.");

            return PexelsBackgrounds.ParsePage(body);
        }
        catch (FormatException ex)
        {
            Log.Warning(ex, "La búsqueda de Pexels devolvió algo que no se pudo leer");
            throw new PexelsSearchException("La búsqueda no respondió bien. Probá de nuevo en un rato.");
        }
        catch (HttpRequestException ex)
        {
            Log.Information(ex, "No se pudo buscar en Pexels");

            // The address of the service does not exist yet (it is not
            // published): that is not the operator's connection.
            if (ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.HostNotFound })
                throw new PexelsSearchException("La búsqueda de fondos todavía no está disponible.");
            throw new PexelsSearchException("No se pudo conectar con la búsqueda. Revisá la conexión a internet.");
        }
        catch (TaskCanceledException) when (!cancel.IsCancellationRequested)
        {
            throw new PexelsSearchException("La búsqueda tardó demasiado. Probá de nuevo.");
        }
    }

    /// <summary>
    /// Downloads the chosen results and brings them into the open tab, the
    /// same way as adding files by hand (thumbnails, light copies…). One that
    /// is already in the library is not downloaded twice. Returns how many
    /// were added and how many failed.
    /// </summary>
    public async Task<(int Added, int Failed)> AddPexelsAsync(
        IReadOnlyList<PexelsHit> hits, string query, IProgress<string> progress, CancellationToken cancel)
    {
        Directory.CreateDirectory(PexelsFolder);
        var ready = new List<string>();
        var failed = 0;

        for (var n = 0; n < hits.Count; n++)
        {
            var hit = hits[n];
            if (hit.File is not { } file || !PexelsBackgrounds.IsPexelsAddress(file.Url))
            {
                failed++;
                continue;
            }

            var path = Path.Combine(PexelsFolder, PexelsBackgrounds.FileName(hit, query));
            if (_allMedia.Any(m => string.Equals(m.Path, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                if (!File.Exists(path))
                    await DownloadPexelsFile(file.Url, path, n + 1, hits.Count, progress, cancel);
                ready.Add(path);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
                                           or TaskCanceledException && !cancel.IsCancellationRequested)
            {
                Log.Warning(ex, "No se pudo bajar {Url} de Pexels", file.Url);
                failed++;
            }
        }

        cancel.ThrowIfCancellationRequested();
        if (ready.Count > 0)
        {
            NoteAction($"Agregar {ready.Count} fondo(s) de Pexels a «{SelectedMediaTab}»");
            ImportMediaFiles(ready);
            Log.Information("Fondos de Pexels agregados: {Count} (fallaron {Failed})", ready.Count, failed);
        }

        return (ready.Count, failed);
    }

    private static async Task DownloadPexelsFile(
        string url, string path, int number, int total, IProgress<string> progress, CancellationToken cancel)
    {
        var partial = path + ".bajando";
        try
        {
            using var response = await PexelsHttp.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel);
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
public sealed class PexelsSearchException(string message) : Exception(message);
