using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// One Bible the program can download: a <c>.ecbiblias</c> with that version
/// alone, where it is, how big it is and its SHA-256 (only that exact file is
/// accepted).
/// </summary>
public sealed record BibleCatalogItem(
    string Name,
    string Abbreviation,
    string Language,
    Uri Url,
    long Size,
    string Sha256,
    string License)
{
    /// <summary>The same version as an entry of a package, to reuse what already reads packages.</summary>
    public BiblePackageEntry AsEntry(string file) => new(file, Name, Abbreviation, Language);
}

/// <summary>
/// The list of Bibles to download (Biblia → 📥 → «Bajar Biblias de internet…»).
/// It is a small JSON next to the Bibles themselves:
/// <code>
/// { "format": 1,
///   "versions": [ { "name": "Reina-Valera 1909", "abbreviation": "RV1909",
///                   "language": "es", "file": "RV1909.ecbiblias",
///                   "size": 1290068, "sha256": "…", "license": "Dominio público" } ] }
/// </code>
/// <c>file</c> is relative to the list (or a full address), so moving them all
/// to another place only changes the address of the list. A new Bible is a new
/// file and a new line: no new version of the program.
/// </summary>
public static class BibleCatalog
{
    public const int Format = 1;

    /// <summary>The name the list has next to the Bibles.</summary>
    public const string FileName = "catalogo.json";

    /// <summary>A list is a few kilobytes; this stops anything else from being read.</summary>
    public const int MaxBytes = 512 * 1024;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>
    /// The Bibles of a list read from <paramref name="catalogUrl"/>. Lines that
    /// are incomplete, or that point anywhere but https (http only on this
    /// computer, for tests), are left out. A list of another format is an error.
    /// </summary>
    public static IReadOnlyList<BibleCatalogItem> Parse(string json, Uri catalogUrl)
    {
        CatalogFile? file;
        try
        {
            file = JsonSerializer.Deserialize<CatalogFile>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException("La lista de Biblias no se pudo leer.", ex);
        }

        if (file is null || file.Format != Format)
            throw new FormatException("La lista de Biblias es de otro formato: hace falta una versión más nueva del programa.");

        var items = new List<BibleCatalogItem>();
        foreach (var line in file.Versions ?? [])
        {
            if (string.IsNullOrWhiteSpace(line.Name) || string.IsNullOrWhiteSpace(line.Abbreviation)
                || string.IsNullOrWhiteSpace(line.File) || line.Size <= 0
                || line.Sha256 is not { Length: 64 } sha || !sha.All(Uri.IsHexDigit))
                continue;

            if (!Uri.TryCreate(catalogUrl, line.File, out var url) || !IsAllowed(url))
                continue;

            items.Add(new BibleCatalogItem(
                line.Name.Trim(),
                line.Abbreviation.Trim(),
                string.IsNullOrWhiteSpace(line.Language) ? "es" : line.Language.Trim().ToLowerInvariant(),
                url,
                line.Size,
                sha.ToLowerInvariant(),
                line.License?.Trim() ?? string.Empty));
        }

        return items;
    }

    /// <summary>The list written out, for whoever prepares the files to upload.</summary>
    public static string Write(IEnumerable<(BibleCatalogItem Item, string File)> versions) =>
        JsonSerializer.Serialize(new CatalogFile
        {
            Format = Format,
            Versions = versions.Select(v => new CatalogLine
            {
                Name = v.Item.Name,
                Abbreviation = v.Item.Abbreviation,
                Language = v.Item.Language,
                File = v.File,
                Size = v.Item.Size,
                Sha256 = v.Item.Sha256,
                License = string.IsNullOrWhiteSpace(v.Item.License) ? null : v.Item.License,
            }).ToList(),
        }, Options);

    /// <summary>Only https, or http to this same computer (to try a list before uploading it).</summary>
    public static bool IsAllowed(Uri url) =>
        url.IsAbsoluteUri
        && (url.Scheme == Uri.UriSchemeHttps || (url.Scheme == Uri.UriSchemeHttp && url.IsLoopback));

    /// <summary>The Bibles grouped by language: Spanish first, then the rest by name; each group by name.</summary>
    public static IReadOnlyList<(string Language, IReadOnlyList<BibleCatalogItem> Items)> ByLanguage(
        IEnumerable<BibleCatalogItem> items) =>
        items
            .GroupBy(i => i.Language)
            .OrderBy(g => g.Key == "es" ? 0 : 1)
            .ThenBy(g => LanguageName(g.Key), StringComparer.CurrentCultureIgnoreCase)
            .Select(g => (LanguageName(g.Key),
                (IReadOnlyList<BibleCatalogItem>)g.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase).ToList()))
            .ToList();

    /// <summary>The name of a language as the operator reads it.</summary>
    public static string LanguageName(string code) => code.ToLowerInvariant() switch
    {
        "es" => "Español",
        "en" => "Inglés",
        "pt" => "Portugués",
        "fr" => "Francés",
        "de" => "Alemán",
        "it" => "Italiano",
        "gn" => "Guaraní",
        "qu" => "Quechua",
        "ht" => "Criollo haitiano",
        _ => code.ToUpperInvariant(),
    };

    private sealed class CatalogFile
    {
        public int Format { get; set; }

        public List<CatalogLine>? Versions { get; set; }
    }

    private sealed class CatalogLine
    {
        public string? Name { get; set; }

        public string? Abbreviation { get; set; }

        public string? Language { get; set; }

        public string? File { get; set; }

        public long Size { get; set; }

        public string? Sha256 { get; set; }

        public string? License { get; set; }
    }
}
