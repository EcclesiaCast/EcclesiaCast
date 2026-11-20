using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Turns the songs Holyrics hands out through its API Server into
/// <see cref="Song"/>s. Holyrics keeps its library in a database whose rows
/// are encrypted, so the only honest way in is the API it documents
/// (github.com/holyrics/API-Server): each song is a JSON object with
/// <c>title</c>, <c>artist</c>, <c>author</c>, <c>copyright</c> and a
/// <c>slides</c> array whose items carry the <c>text</c> and a
/// <c>slide_description</c> such as «Verse 1» or «Chorus».
/// </summary>
public static partial class HolyricsImporter
{
    /// <summary>The fields asked for: just what becomes a song here.</summary>
    public const string Fields = "id,title,artist,author,copyright,archived,slides";

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

    /// <summary>Reads one song object of the API.</summary>
    public static Song FromJson(JsonElement song)
    {
        var lyrics = new StringBuilder();

        if (song.TryGetProperty("slides", out var slides) && slides.ValueKind == JsonValueKind.Array)
        {
            foreach (var slide in slides.EnumerateArray())
            {
                if (slide.ValueKind != JsonValueKind.Object)
                    continue;

                var text = Text(slide, "text").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
                if (text.Length == 0)
                    continue;

                if (lyrics.Length > 0)
                    lyrics.Append("\n\n");

                // A slide without a description goes on under the previous
                // one's label, which is what the editor does with a paragraph
                // that has no [tag] line of its own.
                var label = Label(Text(slide, "slide_description"));
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

    /// <summary>True when the object came with its slides (older Holyrics may leave them out of a list).</summary>
    public static bool HasSlides(JsonElement song) =>
        song.TryGetProperty("slides", out var slides) && slides.ValueKind == JsonValueKind.Array;

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
