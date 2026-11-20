using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcclesiaCast.Core.Songs;

/// <summary>What came out of Holyrics: the songs, and how many were left behind and why.</summary>
public sealed record HolyricsLibrary(IReadOnlyList<Song> Songs, int Archived, int Unreadable);

/// <summary>Holyrics could not be read; <see cref="Exception.Message"/> is meant for the operator.</summary>
public sealed class HolyricsException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Reads the song library of a Holyrics running on this computer through its
/// API Server (File → Settings → API Server in Holyrics). The operator turns
/// on the local server there and creates a token allowed to read songs; with
/// that, every song comes over without touching Holyrics' own files.
/// </summary>
public sealed class HolyricsClient(HttpClient http, int port, string token)
{
    /// <summary>The port Holyrics shows by default for its API Server.</summary>
    public const int DefaultPort = 8091;

    public async Task<HolyricsLibrary> ReadSongsAsync(CancellationToken cancel = default)
    {
        var list = await CallAsync("GetSongs", new { fields = HolyricsImporter.Fields }, cancel);
        if (list.ValueKind != JsonValueKind.Array)
            throw new HolyricsException("Holyrics contestó algo que no es una lista de canciones.");

        var songs = new List<Song>();
        int archived = 0, unreadable = 0;

        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                unreadable++;
                continue;
            }

            if (HolyricsImporter.IsArchived(item))
            {
                archived++;
                continue;
            }

            var full = item;
            if (!HolyricsImporter.HasSlides(item))
            {
                // An older Holyrics lists songs without their words: those
                // are asked for one by one.
                var id = HolyricsImporter.Id(item);
                if (id.Length == 0)
                {
                    unreadable++;
                    continue;
                }

                full = await CallAsync("GetLyrics", new { id, fields = HolyricsImporter.Fields }, cancel);
                if (full.ValueKind != JsonValueKind.Object)
                {
                    unreadable++;
                    continue;
                }
            }

            songs.Add(HolyricsImporter.FromJson(full));
        }

        return new HolyricsLibrary(songs, archived, unreadable);
    }

    /// <summary>One call of the API: POST /api/{action}?token=…, answered with {"status":"ok","data":…}.</summary>
    private async Task<JsonElement> CallAsync(string action, object body, CancellationToken cancel)
    {
        var url = $"http://127.0.0.1:{port}/api/{action}?token={Uri.EscapeDataString(token)}";

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(url, body, cancel);
        }
        catch (HttpRequestException ex)
        {
            throw new HolyricsException(
                $"No pude conectarme con Holyrics en el puerto {port}. Fijate que Holyrics esté abierto "
                + "y que en Archivo → Configuraciones → API Server esté activado el servidor local.", ex);
        }
        catch (TaskCanceledException ex) when (!cancel.IsCancellationRequested)
        {
            throw new HolyricsException(
                $"Holyrics no contestó en el puerto {port}. Fijate que esté abierto y con el API Server activado.", ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new HolyricsException(NoPermission);

            JsonElement root;
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(cancel);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancel);
                root = document.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new HolyricsException(
                    $"En el puerto {port} contestó algo que no es Holyrics. Revisá el número de puerto.", ex);
            }

            var status = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("status", out var s)
                && s.ValueKind == JsonValueKind.String ? s.GetString() : null;

            if (status == "ok")
                return root.TryGetProperty("data", out var data) ? data : default;

            var error = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var e)
                && e.ValueKind == JsonValueKind.String ? e.GetString() ?? string.Empty : string.Empty;

            if (error.Contains("token", StringComparison.OrdinalIgnoreCase))
                throw new HolyricsException(
                    "Holyrics no reconoce ese token. Copialo de nuevo desde Archivo → Configuraciones → API Server.");
            if (error.Contains("permission", StringComparison.OrdinalIgnoreCase)
                || error.Contains("permiss", StringComparison.OrdinalIgnoreCase))
                throw new HolyricsException(NoPermission);

            throw new HolyricsException(error.Length > 0
                ? $"Holyrics no entregó las canciones: {error}"
                : $"En el puerto {port} contestó algo que no es Holyrics. Revisá el número de puerto.");
        }
    }

    private const string NoPermission =
        "El token no tiene permiso para leer canciones. En Holyrics, en Archivo → Configuraciones → API Server, "
        + "entrá a los permisos de ese token y habilitá «GetSongs» y «GetLyrics».";
}

/// <summary>Finds whether Holyrics is installed, to offer it in the import window.</summary>
public static class HolyricsLocator
{
    /// <summary>The first of the given folders that holds Holyrics.exe, or null.</summary>
    public static string? FindInstall(IEnumerable<string?> folders)
    {
        foreach (var folder in folders)
        {
            if (!string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, "Holyrics.exe")))
                return folder;
        }

        return null;
    }

    /// <summary>Where the Holyrics installer puts it: C:\Holyrics by default.</summary>
    public static IEnumerable<string?> DefaultFolders()
    {
        var system = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        yield return Path.Combine(string.IsNullOrEmpty(system) ? @"C:\" : system, "Holyrics");
        yield return Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Holyrics");
        yield return Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Holyrics");
        yield return Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Holyrics");
        yield return Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Holyrics");
    }

    private static string? Combine(string root, params string[] parts) =>
        string.IsNullOrEmpty(root) ? null : Path.Combine([root, .. parts]);
}
