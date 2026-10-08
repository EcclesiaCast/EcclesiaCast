using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// Which background the PREVIEW («Siguiente») draws behind the next slide.
/// Projecting a slide takes a foreground media (an announcement) off the
/// screen, so the next slide will not have it behind: the preview must show
/// the lyrics on what will be left, never on top of the announcement.
/// </summary>
public static class NextSlideBackground
{
    /// <summary>The background that stays after the next slide goes live.</summary>
    public static MediaItem? Resolve(MediaItem? current) =>
        current is { Behavior: MediaBehavior.Foreground } ? null : current;
}
