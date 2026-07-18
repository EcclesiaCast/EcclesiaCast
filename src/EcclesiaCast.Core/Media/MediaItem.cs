namespace EcclesiaCast.Core.Media;

public enum MediaType
{
    Image,
    Video,

    /// <summary>A YouTube video, played in the embedded browser with the operator's own session.</summary>
    YouTube,
}

/// <summary>How a non-16:9 asset is fitted to the 16:9 output.</summary>
public enum MediaScaling
{
    /// <summary>Fill the screen, cropping the overflow (keeps proportions).</summary>
    Fill,

    /// <summary>Fit the whole asset with bars (keeps proportions).</summary>
    Fit,

    /// <summary>Stretch to the screen, ignoring proportions.</summary>
    Stretch,
}

/// <summary>Which layer the media plays on, ProPresenter-style.</summary>
public enum MediaBehavior
{
    /// <summary>Loops behind the text; changing slides doesn't restart it.</summary>
    Background,

    /// <summary>Full screen over everything, hiding the text (announcements, trailers).</summary>
    Foreground,
}

/// <summary>What a video does when it reaches the end.</summary>
public enum VideoEndBehavior
{
    Loop,

    /// <summary>Play once and stop (last frame / black).</summary>
    Stop,

    /// <summary>Play once and switch the output to the church logo.</summary>
    Logo,
}

/// <summary>A background asset in the media library (an image or a video).</summary>
public sealed class MediaItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the source file, or the watch URL for YouTube.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>YouTube video id when <see cref="Type"/> is YouTube.</summary>
    public string? YouTubeId { get; set; }

    public MediaType Type { get; set; }

    /// <summary>Absolute path to a generated thumbnail (may be null for images, which self-thumbnail).</summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>Tab this item belongs to (Fondos, Anuncios, Jóvenes…).</summary>
    public string Category { get; set; } = "Fondos";

    public MediaScaling Scaling { get; set; } = MediaScaling.Fill;

    public MediaBehavior Behavior { get; set; } = MediaBehavior.Background;

    public VideoEndBehavior EndBehavior { get; set; } = VideoEndBehavior.Loop;

    /// <summary>Mute the video's audio (backgrounds are muted by default).</summary>
    public bool Muted { get; set; } = true;

    /// <summary>Playback volume 0–100 when not muted.</summary>
    public int Volume { get; set; } = 100;
}
