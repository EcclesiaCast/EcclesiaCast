namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// The platform services, for the static helpers (<c>Notice</c>, the
/// importers, the sound watcher) that have no constructor to receive them.
/// Each interface fills these in first thing at start-up, before anything
/// else runs; the rest gets the same objects through dependency injection.
/// </summary>
public static class PlatformServices
{
    private static IPlatformPaths? _paths;

    /// <summary>The screen's thread; null when there is no screen (the tests).</summary>
    public static IUiDispatcher? Dispatcher { get; set; }

    /// <summary>Clocks on the screen's thread; null when there is no screen.</summary>
    public static IUiTimerFactory? Timers { get; set; }

    /// <summary>Notices and questions; null when there is no screen.</summary>
    public static INotices? Notices { get; set; }

    /// <summary>The system's volume for the whole program; null where it has none (see <see cref="IProgramVolume"/>).</summary>
    public static IProgramVolume? ProgramVolume { get; set; }

    /// <summary>The shared video engine; null until it was started.</summary>
    public static IVideoEngine? Video { get; set; }

    /// <summary>PDF pages; null where nothing draws them.</summary>
    public static IDocumentRenderer? Documents { get; set; }

    /// <summary>Decks to PDF; null where nothing converts them.</summary>
    public static IDeckConverter? Decks { get; set; }

    public static IPlatformPaths Paths
    {
        get => _paths ?? throw new InvalidOperationException(
            "Platform paths are not set: the program sets them first thing at start-up.");
        set => _paths = value;
    }
}
