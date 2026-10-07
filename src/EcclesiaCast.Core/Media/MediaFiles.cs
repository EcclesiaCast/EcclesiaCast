namespace EcclesiaCast.Core.Media;

/// <summary>What the media library does with a file, judged by its name.</summary>
public enum MediaFileKind
{
    /// <summary>Not something the library takes.</summary>
    Unsupported,
    Image,
    Video,

    /// <summary>A PDF or PowerPoint deck: it becomes one image per page.</summary>
    Document,
}

/// <summary>
/// The one list of files the media bar accepts, shared by the «Agregar»
/// dialog and by dropping files from Explorer so the two can never disagree.
/// </summary>
public static class MediaFiles
{
    private static readonly HashSet<string> Images =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };

    private static readonly HashSet<string> Videos =
        new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".m4v", ".avi", ".mkv", ".wmv", ".webm" };

    private static readonly HashSet<string> Documents =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".pptx", ".ppt" };

    public static MediaFileKind Classify(string path)
    {
        var extension = Path.GetExtension(path);
        if (Images.Contains(extension))
            return MediaFileKind.Image;
        if (Videos.Contains(extension))
            return MediaFileKind.Video;
        if (Documents.Contains(extension))
            return MediaFileKind.Document;
        return MediaFileKind.Unsupported;
    }
}
