using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace EcclesiaCast.Core.Bible;

/// <summary>One Bible version offered by a package: what it is called and which file of the zip holds it.</summary>
public sealed record BiblePackageEntry(string File, string Name, string Abbreviation, string Language);

/// <summary>
/// The <c>.ecbiblias</c> file: a zip with one JSON per version (the shape of
/// <see cref="JsonBibleExporter"/>) and a <c>manifest.json</c> that says what
/// each one is. The installer carries the free Bibles this way, and the
/// operator moves the ones that have rights from one computer to another.
///
/// A package is data that comes from outside, so nothing in it is ever written
/// to disk by path: entries are looked up by name inside the zip and read
/// into memory under a size cap.
/// </summary>
public static class BiblePackage
{
    public const string Extension = ".ecbiblias";
    public const string ManifestName = "manifest.json";

    /// <summary>A full Bible is about 5 MB of JSON; this leaves room for large ones and stops a zip bomb.</summary>
    private const long MaxVersionBytes = 64L * 1024 * 1024;
    private const long MaxManifestBytes = 1024 * 1024;
    private const int MaxVersions = 100;
    private const int MaxTextLength = 200;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Writes a package with the given versions. The file names inside the zip are numbered.</summary>
    public static void Write(
        Stream output,
        IEnumerable<(string Name, string Abbreviation, string Language, ParsedBible Bible)> versions)
    {
        var manifest = new List<object>();
        using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);

        var number = 0;
        foreach (var (name, abbreviation, language, bible) in versions)
        {
            number++;
            var file = $"version-{number:00}.json";
            manifest.Add(new { file, name, abbreviation, language });

            var entry = zip.CreateEntry(file, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(JsonBibleExporter.ToJson(bible));
        }

        var manifestEntry = zip.CreateEntry(ManifestName, CompressionLevel.Optimal);
        using var manifestWriter = new StreamWriter(manifestEntry.Open(), new UTF8Encoding(false));
        manifestWriter.Write(JsonSerializer.Serialize(new { format = 1, versions = manifest }, WriteOptions));
    }

    /// <summary>
    /// The versions a package offers. Entries with a missing name or
    /// abbreviation, pointing to a file that is not in the zip, or repeating an
    /// abbreviation already listed are left out. Throws <see cref="FormatException"/>
    /// when the file is not a package at all.
    /// </summary>
    public static IReadOnlyList<BiblePackageEntry> ReadManifest(Stream package)
    {
        try
        {
            using var zip = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: true);
            var manifestEntry = zip.GetEntry(ManifestName)
                ?? throw new FormatException("El archivo no es un paquete de Biblias: falta el manifiesto.");

            using var doc = JsonDocument.Parse(ReadCapped(manifestEntry, MaxManifestBytes));
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("versions", out var versionsEl)
                || versionsEl.ValueKind != JsonValueKind.Array)
                throw new FormatException("El manifiesto del paquete no tiene la lista de versiones.");

            var entries = new List<BiblePackageEntry>();
            foreach (var el in versionsEl.EnumerateArray())
            {
                if (entries.Count >= MaxVersions)
                    break;
                if (el.ValueKind != JsonValueKind.Object)
                    continue;

                var file = Text(el, "file");
                var name = Text(el, "name");
                var abbreviation = Text(el, "abbreviation");
                var language = Text(el, "language");
                if (file.Length == 0 || name.Length == 0 || abbreviation.Length == 0)
                    continue;
                if (zip.GetEntry(file) is null)
                    continue;
                if (entries.Any(e => BibleInstallPlanner.SameKey(e.Abbreviation, abbreviation)))
                    continue;

                entries.Add(new BiblePackageEntry(file, name, abbreviation, language.Length == 0 ? "es" : language));
            }

            return entries;
        }
        catch (InvalidDataException ex)
        {
            throw new FormatException("El archivo no es un paquete de Biblias (no es un zip válido).", ex);
        }
        catch (JsonException ex)
        {
            throw new FormatException("El manifiesto del paquete está dañado.", ex);
        }
    }

    /// <summary>Reads one version of the package.</summary>
    public static ParsedBible ReadVersion(Stream package, BiblePackageEntry entry)
    {
        try
        {
            using var zip = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: true);
            var zipEntry = zip.GetEntry(entry.File)
                ?? throw new FormatException($"El paquete no trae el archivo de «{entry.Name}».");
            var json = ReadCapped(zipEntry, MaxVersionBytes);
            return JsonBibleImporter.Parse(Encoding.UTF8.GetString(json));
        }
        catch (InvalidDataException ex)
        {
            throw new FormatException("El paquete está dañado.", ex);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"La Biblia «{entry.Name}» del paquete está dañada.", ex);
        }
    }

    private static string Text(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var el) || el.ValueKind != JsonValueKind.String)
            return string.Empty;
        var text = (el.GetString() ?? string.Empty).Trim();
        return text.Length > MaxTextLength ? text[..MaxTextLength] : text;
    }

    /// <summary>
    /// Reads a zip entry into memory, giving up when it holds more than
    /// <paramref name="max"/> bytes (the size the zip declares can lie).
    /// </summary>
    private static byte[] ReadCapped(ZipArchiveEntry entry, long max)
    {
        if (entry.Length > max)
            throw new FormatException("El paquete trae un archivo demasiado grande.");

        using var source = entry.Open();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = source.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > max)
                throw new FormatException("El paquete trae un archivo demasiado grande.");
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
