namespace EcclesiaCast.Presentation.Platform;

/// <summary>Where EcclesiaCast keeps its things on this system.</summary>
public interface IPlatformPaths
{
    /// <summary>
    /// The library, thumbnails and imported pages. A test build pointed at a
    /// copy (ECCLESIACAST_DATA_DIR) must keep <i>all</i> of it there: the
    /// thumbnail sweep deletes every PNG its library does not reference, so
    /// one that looked in the real folder would empty it.
    /// </summary>
    string DataFolder { get; }

    /// <summary>Default home for downloaded videos.</summary>
    string DownloadsFolder { get; }

    /// <summary>Where the helper programs live (yt-dlp and what it needs).</summary>
    string ToolsFolder { get; }

    /// <summary>The speech recogniser's models.</summary>
    string ModelsFolder { get; }

    /// <summary>
    /// The light copies of heavy videos: a cache, regenerable, not user data.
    /// A test build pointed at a copy of a library keeps its copies there too.
    /// </summary>
    string LightCopiesFolder { get; }

    /// <summary>What comes with the program (the bundled Bibles and backgrounds).</summary>
    string BundledFolder { get; }
}
