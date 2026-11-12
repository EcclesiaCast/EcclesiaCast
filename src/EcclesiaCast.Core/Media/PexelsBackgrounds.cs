using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcclesiaCast.Core.Media;

/// <summary>The file of a result that the program downloads.</summary>
public sealed record PexelsFile(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("ancho")] int Width,
    [property: JsonPropertyName("alto")] int Height);

/// <summary>A video or a photo found on Pexels, as our service hands it over.</summary>
public sealed record PexelsHit(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("tipo")] string Kind,
    [property: JsonPropertyName("ancho")] int Width,
    [property: JsonPropertyName("alto")] int Height,
    [property: JsonPropertyName("duracion")] double Duration,
    [property: JsonPropertyName("miniatura")] string Thumbnail,
    [property: JsonPropertyName("autor")] string? Author,
    [property: JsonPropertyName("autorUrl")] string? AuthorUrl,
    [property: JsonPropertyName("paginaUrl")] string? PageUrl,
    [property: JsonPropertyName("archivo")] PexelsFile? File)
{
    public bool IsVideo => Kind == "video";

    /// <summary>«0:12» for a video; empty for a photo.</summary>
    public string DurationText =>
        IsVideo && Duration > 0 ? TimeSpan.FromSeconds(Math.Round(Duration)).ToString(@"m\:ss", CultureInfo.InvariantCulture) : string.Empty;
}

/// <summary>A page of results.</summary>
public sealed record PexelsPage(
    [property: JsonPropertyName("resultados")] IReadOnlyList<PexelsHit> Hits,
    [property: JsonPropertyName("hayMas")] bool HasMore);

/// <summary>
/// The rules for backgrounds from Pexels: what our service answers, which
/// addresses the program accepts to download from, and the file name each
/// one gets in the library.
/// </summary>
public static class PexelsBackgrounds
{
    /// <summary>What the search window shows under the results, as Pexels asks.</summary>
    public const string Credit = "Fotos y videos de Pexels";

    public const string Site = "https://www.pexels.com";

    /// <summary>
    /// Reads a page of results. Throws <see cref="FormatException"/> when the
    /// answer is not one. Results without a file or a thumbnail, or whose
    /// file is not on Pexels, are left out.
    /// </summary>
    public static PexelsPage ParsePage(string json)
    {
        PexelsPage? page;
        try
        {
            page = JsonSerializer.Deserialize<PexelsPage>(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException("La respuesta de la búsqueda no se pudo leer.", ex);
        }

        if (page?.Hits is null)
            throw new FormatException("La respuesta de la búsqueda no se pudo leer.");

        var usable = page.Hits
            .Where(h => h.File is { } f && IsPexelsAddress(f.Url) && IsPexelsAddress(h.Thumbnail))
            .ToList();
        return page with { Hits = usable };
    }

    /// <summary>The error our service explains in its answer («Probá en un rato»), if any.</summary>
    public static string? ErrorOf(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Only files from Pexels are downloaded, over https: an answer that
    /// pointed somewhere else would not be followed.
    /// </summary>
    public static bool IsPexelsAddress(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && (uri.Host.Equals("pexels.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".pexels.com", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The file name in the library: what was searched, who made it and its
    /// number on Pexels, so it reads well in the media bar and can be found on
    /// Pexels again. «Cruz atardecer · Ana López · Pexels 12345.mp4».
    /// </summary>
    public static string FileName(PexelsHit hit, string query)
    {
        var what = Clean(query);
        what = what.Length == 0 ? (hit.IsVideo ? "Video" : "Foto") : char.ToUpper(what[0], CultureInfo.CurrentCulture) + what[1..];
        var who = Clean(hit.Author ?? string.Empty);
        var name = who.Length > 0 ? $"{what} · {who} · Pexels {hit.Id}" : $"{what} · Pexels {hit.Id}";
        return name + (hit.IsVideo ? ".mp4" : ".jpg");
    }

    /// <summary>Without what Windows does not allow in a file name, without doubled spaces, at most 40 letters.</summary>
    private static string Clean(string text)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var kept = new StringBuilder(text.Length);
        foreach (var ch in text.Trim())
        {
            if (invalid.Contains(ch) || char.IsControl(ch))
                continue;
            if (ch == ' ' && kept.Length > 0 && kept[^1] == ' ')
                continue;
            kept.Append(ch);
        }

        var clean = kept.ToString().Trim();
        return clean.Length > 40 ? clean[..40].TrimEnd() : clean;
    }
}
