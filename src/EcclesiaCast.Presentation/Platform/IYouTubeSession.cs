namespace EcclesiaCast.Presentation.Platform;

/// <summary>The session of the program's own browser on YouTube, for the videos that ask for an account.</summary>
public interface IYouTubeSession
{
    /// <summary>
    /// Writes the session to a temporary file for yt-dlp (the caller deletes
    /// it); null when there is none to give. Call it on the screen's thread,
    /// where the browser lives.
    /// </summary>
    Task<string?> ExportCookiesAsync();
}
