namespace EcclesiaCast.Core.Audio;

/// <summary>
/// The sound files the Música tab takes, judged by their name. One list for
/// the «Agregar» dialog and for dropping files from Explorer, so the two
/// cannot disagree.
/// </summary>
public static class AudioFiles
{
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".m4a", ".ogg", ".flac" };

    public static bool IsAudio(string path) => Extensions.Contains(System.IO.Path.GetExtension(path));

    /// <summary>The name a track gets in the list: the file name without its extension.</summary>
    public static string DisplayName(string path)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(path).Trim();
        return name.Length > 0 ? name : System.IO.Path.GetFileName(path);
    }
}
