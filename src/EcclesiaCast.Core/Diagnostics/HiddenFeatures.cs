namespace EcclesiaCast.Core.Diagnostics;

/// <summary>
/// Functions that are still being tried out and are hidden from everyone
/// except where an empty text file with their name sits in the data folder
/// («seguir-la-cancion.txt»). Its content does not matter. It is looked at
/// once, when the program starts: to turn a function on or off the program
/// has to be closed and opened again.
/// </summary>
public static class HiddenFeatures
{
    /// <summary>«Seguir la canción por lo que se escucha» (microphone and speech recogniser).</summary>
    public const string FollowTheSong = "seguir-la-cancion";

    /// <summary>Whether <c>&lt;dataFolder&gt;\&lt;feature&gt;.txt</c> exists.</summary>
    public static bool IsEnabled(string dataFolder, string feature)
    {
        if (string.IsNullOrWhiteSpace(dataFolder) || string.IsNullOrWhiteSpace(feature))
            return false;

        try
        {
            return File.Exists(Path.Combine(dataFolder, feature + ".txt"));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}
