using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Presentation;

/// <summary>What is on the screen right now, as the operator thinks of it: which section of the window it came from.</summary>
public enum OnAirSource
{
    /// <summary>The output is off, black, on the logo, or empty.</summary>
    None,

    /// <summary>A section of a song (the grid holds the song).</summary>
    Song,

    /// <summary>A verse (the grid holds the chapter).</summary>
    Bible,

    /// <summary>The quick text.</summary>
    QuickText,

    /// <summary>A picture or video from the media bar, alone on the screen.</summary>
    Media,

    /// <summary>A web page, as a photo or live.</summary>
    Web,

    /// <summary>A page of a presentation opened in the grid.</summary>
    Presentation,
}

/// <summary>Where the words that are live came from.</summary>
public enum SlideOrigin
{
    None,
    Song,
    Bible,
    QuickText,
}

/// <summary>
/// Works out <see cref="OnAirSource"/> from the real state of the output, so
/// «EN VIVO» marks follow what the congregation sees instead of the last
/// button pressed: after a verse, a click on a page of a presentation takes
/// the whole section of the verses off the air.
/// </summary>
public static class OnAirResolver
{
    /// <param name="isProjecting">The output window is up.</param>
    /// <param name="state">What the output shows: words, background only, black or logo.</param>
    /// <param name="liveSlide">Where the words in <paramref name="state"/> Content came from.</param>
    /// <param name="background">The media on screen, if any.</param>
    /// <param name="backgroundIsPresentationPage">That media is a page of a presentation.</param>
    public static OnAirSource Resolve(
        bool isProjecting, OutputState state, SlideOrigin liveSlide,
        MediaItem? background, bool backgroundIsPresentationPage)
    {
        if (!isProjecting || state is OutputState.Black or OutputState.Logo)
            return OnAirSource.None;

        if (background is { Type: MediaType.Web })
            return OnAirSource.Web;

        if (background is not null && backgroundIsPresentationPage)
            return OnAirSource.Presentation;

        // Alone on the screen: a foreground, or any background once the words are cleared.
        if (background is not null && (background.Behavior == MediaBehavior.Foreground || state == OutputState.Clear))
            return OnAirSource.Media;

        if (state == OutputState.Content)
        {
            return liveSlide switch
            {
                SlideOrigin.Song => OnAirSource.Song,
                SlideOrigin.Bible => OnAirSource.Bible,
                SlideOrigin.QuickText => OnAirSource.QuickText,
                _ => OnAirSource.None,
            };
        }

        return OnAirSource.None;
    }
}
