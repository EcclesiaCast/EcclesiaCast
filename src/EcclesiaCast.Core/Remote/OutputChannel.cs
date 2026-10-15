using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Remote;

/// <summary>
/// The streaming page can show everything that is on the projector, or only
/// one kind of thing, so the church can put the Bible, the songs and the
/// announcements on three separate layers of its stream (and cut each in or
/// out on its own).
/// </summary>
public enum OutputChannel
{
    /// <summary>Everything, as on the projector.</summary>
    All,

    /// <summary>Only verses.</summary>
    Bible,

    /// <summary>Only the words of songs.</summary>
    Songs,

    /// <summary>Quick text, pictures and videos, pages of a presentation, the lower third and the countdown.</summary>
    Announcements,
}

public static class OutputChannels
{
    /// <summary>The address of the page of each channel, after the PIN-less part: «/salida», «/salida/biblia»…</summary>
    public static string Path(OutputChannel channel) => channel switch
    {
        OutputChannel.Bible => "/salida/biblia",
        OutputChannel.Songs => "/salida/canciones",
        OutputChannel.Announcements => "/salida/anuncios",
        _ => "/salida",
    };

    /// <summary>The channel of an address of the streaming page, or null when it is not one.</summary>
    public static OutputChannel? FromPath(string? path)
    {
        var clean = (path ?? string.Empty).Split('?')[0].TrimEnd('/').ToLowerInvariant();
        return clean switch
        {
            "/salida" or "/output" => OutputChannel.All,
            "/salida/biblia" => OutputChannel.Bible,
            "/salida/canciones" => OutputChannel.Songs,
            "/salida/anuncios" => OutputChannel.Announcements,
            _ => null,
        };
    }

    /// <summary>The channel named in a query («canal=biblia»); everything when it is missing or unknown.</summary>
    public static OutputChannel FromName(string? name) => (name ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "biblia" => OutputChannel.Bible,
        "canciones" => OutputChannel.Songs,
        "anuncios" => OutputChannel.Announcements,
        _ => OutputChannel.All,
    };

    /// <summary>The name used in the query, the opposite of <see cref="FromName"/>.</summary>
    public static string Name(OutputChannel channel) => channel switch
    {
        OutputChannel.Bible => "biblia",
        OutputChannel.Songs => "canciones",
        OutputChannel.Announcements => "anuncios",
        _ => "todo",
    };

    /// <summary>True when the words or the picture that are on air belong on this channel's page.</summary>
    public static bool Shows(OutputChannel channel, OnAirSource source) => channel switch
    {
        OutputChannel.All => source != OnAirSource.None,
        OutputChannel.Bible => source == OnAirSource.Bible,
        OutputChannel.Songs => source == OnAirSource.Song,
        OutputChannel.Announcements => source is OnAirSource.QuickText or OnAirSource.Media
            or OnAirSource.Presentation or OnAirSource.Web,
        _ => false,
    };

    /// <summary>
    /// True when the lower third and the countdown go on this page. They are
    /// announcements, so they ride on that page (and on the one with everything)
    /// and not on the Bible or the songs.
    /// </summary>
    public static bool ShowsOverlays(OutputChannel channel) =>
        channel is OutputChannel.All or OutputChannel.Announcements;
}
