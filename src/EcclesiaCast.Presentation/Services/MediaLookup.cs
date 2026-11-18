using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// The media library, for the places that draw a logo: a logo can fill the
/// screen around itself with an image of the library, and the logo layer and
/// the logo window have no view model of their own to ask. The main view
/// model plugs itself in when it starts.
/// </summary>
public static class MediaLookup
{
    public static Func<int, MediaItem?> Find { get; set; } = _ => null;

    public static Func<IReadOnlyList<MediaItem>> All { get; set; } = () => [];
}
