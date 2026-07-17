namespace EcclesiaCast.Core.Media;

public enum MediaType
{
    Image,
    Video,
}

/// <summary>A background asset in the media library (an image or a video loop).</summary>
public sealed class MediaItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the source file.</summary>
    public string Path { get; set; } = string.Empty;

    public MediaType Type { get; set; }

    /// <summary>Absolute path to a generated thumbnail (may be null for images, which self-thumbnail).</summary>
    public string? ThumbnailPath { get; set; }
}
