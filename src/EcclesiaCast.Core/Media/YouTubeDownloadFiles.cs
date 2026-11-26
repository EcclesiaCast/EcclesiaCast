using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Media;

/// <summary>
/// The files yt-dlp leaves in the downloads folder, named
/// «Título [idDelVideo].mp4». When a video had been downloaded before (it was
/// taken out of the library but its file stayed), yt-dlp skips it and says
/// nothing: the file already there is the download.
/// </summary>
public static partial class YouTubeDownloadFiles
{
    private static readonly string[] VideoExtensions = [".mp4", ".mkv", ".webm", ".mov", ".m4v"];

    /// <summary>
    /// The finished video of <paramref name="videoId"/> among <paramref name="paths"/>,
    /// or null. Half downloads (<c>.part</c>) and the separate pieces yt-dlp
    /// joins (<c>.f137.mp4</c>) are not it.
    /// </summary>
    public static string? AlreadyDownloaded(IEnumerable<string> paths, string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
            return null;

        var tag = $"[{videoId}]";
        return paths
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                var extension = Path.GetExtension(name);
                var stem = Path.GetFileNameWithoutExtension(name);
                return VideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                       && stem.EndsWith(tag, StringComparison.Ordinal)
                       && !Piece().IsMatch(name);
            })
            .FirstOrDefault();
    }

    /// <summary>«… [id].f137.mp4»: one of the two halves before they are joined.</summary>
    [GeneratedRegex(@"\.f\d+\.[A-Za-z0-9]+$")]
    private static partial Regex Piece();
}
