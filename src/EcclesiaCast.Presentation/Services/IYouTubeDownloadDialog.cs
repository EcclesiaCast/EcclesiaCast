namespace EcclesiaCast.Presentation.Services;

/// <summary>The window that downloads a YouTube video to a file while the operator waits.</summary>
public interface IYouTubeDownloadDialog
{
    /// <summary>
    /// Downloads the video and returns when the window closes: the path of the
    /// file, or null when the download did not finish.
    /// </summary>
    string? Download(string videoId, string name);
}
