namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Imports a song from a plain-text file: the file name is the title
/// (with an optional " - Artista" suffix) and the content follows the
/// same rules as the editor (one paragraph per slide).
/// </summary>
public static class TxtSongImporter
{
    public static Song FromText(string fileName, string content)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).Trim();
        var title = name;
        var artist = string.Empty;

        var separator = name.IndexOf(" - ", StringComparison.Ordinal);
        if (separator > 0)
        {
            title = name[..separator].Trim();
            artist = name[(separator + 3)..].Trim();
        }

        return new Song
        {
            Title = title.Length > 0 ? title : "Sin título",
            Artist = artist,
            Sections = LyricsParser.Parse(content),
        };
    }
}
