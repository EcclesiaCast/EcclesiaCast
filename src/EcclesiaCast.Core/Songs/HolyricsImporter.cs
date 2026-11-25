using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>A Holyrics file could not be read; <see cref="Exception.Message"/> is meant for the operator.</summary>
public sealed class HolyricsException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Turns the songs Holyrics saves (Canción → Exportar, or Archivo → Guardar) into
/// <see cref="Song"/>s. Holyrics keeps its library encrypted, but the free plan can
/// save it as a .json (a list of songs, each with <c>title</c>, <c>artist</c> and
/// <c>lyrics.paragraphs</c>) or a .txt, and those are what is read here.
/// </summary>
public static partial class HolyricsImporter
{
    [GeneratedRegex(@"^(?<name>.*?)\s*(?<number>\d+)?$")]
    private static partial Regex NameAndNumber();

    /// <summary>
    /// Holyrics names its slides in the language of whoever set it up, most
    /// often Portuguese or English. They are given the names the song editor
    /// uses, so a chorus imported from Holyrics looks like one written here.
    /// </summary>
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["verse"] = LyricsTags.Verse,
        ["verso"] = LyricsTags.Verse,
        ["estrofe"] = LyricsTags.Verse,
        ["estrofa"] = LyricsTags.Verse,
        ["chorus"] = LyricsTags.Chorus,
        ["refrain"] = LyricsTags.Chorus,
        ["refrão"] = LyricsTags.Chorus,
        ["refrao"] = LyricsTags.Chorus,
        ["coro"] = LyricsTags.Chorus,
        ["estribillo"] = LyricsTags.Chorus,
        ["pre-chorus"] = "Pre-coro",
        ["pre chorus"] = "Pre-coro",
        ["prechorus"] = "Pre-coro",
        ["pré-refrão"] = "Pre-coro",
        ["pre-refrão"] = "Pre-coro",
        ["pré refrão"] = "Pre-coro",
        ["pre-coro"] = "Pre-coro",
        ["precoro"] = "Pre-coro",
        ["bridge"] = LyricsTags.Bridge,
        ["ponte"] = LyricsTags.Bridge,
        ["puente"] = LyricsTags.Bridge,
        ["ending"] = LyricsTags.Ending,
        ["outro"] = LyricsTags.Ending,
        ["final"] = LyricsTags.Ending,
        ["finalização"] = LyricsTags.Ending,
        ["intro"] = "Intro",
        ["introdução"] = "Intro",
        ["introducción"] = "Intro",
        ["interlude"] = "Interludio",
        ["interlúdio"] = "Interludio",
        ["interludio"] = "Interludio",
    };

    /// <summary>Reads one song object of a Holyrics .json.</summary>
    public static Song FromJson(JsonElement song)
    {
        var lyrics = new StringBuilder();

        if (song.TryGetProperty("lyrics", out var body) && body.ValueKind == JsonValueKind.Object
            && body.TryGetProperty("paragraphs", out var paragraphs) && paragraphs.ValueKind == JsonValueKind.Array)
        {
            foreach (var paragraph in paragraphs.EnumerateArray())
            {
                if (paragraph.ValueKind != JsonValueKind.Object)
                    continue;

                var text = Text(paragraph, "text").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
                if (text.Length == 0)
                    continue;

                if (lyrics.Length > 0)
                    lyrics.Append("\n\n");

                var label = Label(Text(paragraph, "description"));
                if (label.Length > 0)
                    lyrics.Append('[').Append(label).Append("]\n");

                lyrics.Append(text);
            }
        }

        var title = Text(song, "title");
        var artist = Text(song, "artist");
        var copyright = Text(song, "copyright");

        return new Song
        {
            Title = title.Length > 0 ? title : "Sin título",
            Artist = artist.Length > 0 ? artist : Text(song, "author"),
            Copyright = copyright.Length > 0 ? copyright : null,
            Sections = LyricsParser.Parse(lyrics.ToString()),
        };
    }

    /// <summary>
    /// Reads a file saved from Holyrics' free «Guardar» / «Exportación en lote»:
    /// a .json (one song or a list of them) or a .txt («Título:», «Artista:» and
    /// the words). Holyrics' own welcome song is left out.
    /// </summary>
    public static IReadOnlyList<Song> FromFile(string path)
    {
        var raw = File.ReadAllText(path, Encoding.UTF8);
        var songs = new List<Song>();

        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(raw, new JsonDocumentOptions { AllowTrailingCommas = true });
            }
            catch (JsonException)
            {
                throw new HolyricsException($"«{Path.GetFileName(path)}» no es un archivo JSON de Holyrics.");
            }

            using (document)
            {
                var root = document.RootElement;
                var items = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : [root];
                foreach (var item in items.Where(i => i.ValueKind == JsonValueKind.Object))
                {
                    if (IsWelcomeSong(item) || IsArchived(item))
                        continue;
                    songs.Add(FromJson(item));
                }
            }
        }
        else
        {
            songs.Add(FromText(raw, Path.GetFileNameWithoutExtension(path)));
        }

        return songs.Where(s => s.Sections.Count > 0).ToList();
    }

    /// <summary>The .txt of Holyrics: «Título: …», «Artista: …», a blank line and the words.</summary>
    public static Song FromText(string text, string fallbackTitle)
    {
        var lines = text.TrimStart('﻿').Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string title = string.Empty, artist = string.Empty;
        var index = 0;

        for (; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0)
                break;

            var colon = line.IndexOf(':');
            var key = colon > 0 ? line[..colon].Trim().ToLowerInvariant() : string.Empty;
            var value = colon > 0 ? line[(colon + 1)..].Trim() : string.Empty;
            if (key is "título" or "titulo" or "title")
                title = value;
            else if (key is "artista" or "artist" or "autor" or "author")
                artist = value;
            else
                break; // not a header line: the words start here
        }

        var body = string.Join("\n", lines.Skip(index)).Trim();
        return new Song
        {
            Title = title.Length > 0 ? title : fallbackTitle,
            Artist = artist,
            Sections = LyricsParser.Parse(body),
        };
    }

    /// <summary>The song every Holyrics brings to say hello: not the church's.</summary>
    private static bool IsWelcomeSong(JsonElement song) =>
        Id(song) == "1611686353330"
        || (Text(song, "artist") == "Software & App" && Text(song, "title").Equals("Holyrics", StringComparison.OrdinalIgnoreCase));

    /// <summary>Archived songs are the ones the church put away in Holyrics; they are not brought over.</summary>
    public static bool IsArchived(JsonElement song) =>
        song.TryGetProperty("archived", out var archived) && archived.ValueKind == JsonValueKind.True;

    /// <summary>The song's id as text, whether Holyrics wrote it as a string or as a number.</summary>
    public static string Id(JsonElement song) =>
        song.TryGetProperty("id", out var id)
            ? id.ValueKind switch
            {
                JsonValueKind.String => id.GetString() ?? string.Empty,
                JsonValueKind.Number => id.GetRawText(),
                _ => string.Empty,
            }
            : string.Empty;

    /// <summary>«Verse 1» → «Estrofa 1», «Refrão» → «Coro»; a name it does not know stays as it is.</summary>
    public static string Label(string description)
    {
        var clean = description.Replace('[', '(').Replace(']', ')').Trim().TrimEnd(':').Trim();
        if (clean.Length == 0)
            return string.Empty;

        var match = NameAndNumber().Match(clean);
        var name = match.Groups["name"].Value.Trim();
        if (!Names.TryGetValue(name, out var spanish))
            return clean;

        return match.Groups["number"].Success ? $"{spanish} {match.Groups["number"].Value}" : spanish;
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? (value.GetString() ?? string.Empty).Trim()
            : string.Empty;
}
