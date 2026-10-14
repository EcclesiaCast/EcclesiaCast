using System.Globalization;

namespace EcclesiaCast.Core.Media;

/// <summary>
/// Posters and announcements are about a day: once the event has passed they
/// only get in the way. A media can carry the date of its event, and a number
/// of days after it; when that day arrives it goes to the tab «Eventos pasados»
/// by itself (the file is not touched: it stays in the library, one tab over).
/// </summary>
public static class MediaExpiry
{
    /// <summary>The tab the media of past events are sent to.</summary>
    public const string ArchiveTab = "Eventos pasados";

    /// <summary>How many days a poster stays after its event unless told otherwise.</summary>
    public const int DefaultDays = 10;

    /// <summary>The day the media goes to <see cref="ArchiveTab"/>: the event's date plus its days; null without an event date.</summary>
    public static DateTime? MovesOn(DateTime? eventDate, int days) =>
        eventDate?.Date.AddDays(Math.Max(0, days));

    /// <summary>True from the day it moves on: «stays ten days» means gone on the tenth.</summary>
    public static bool IsDue(MediaItem media, DateTime today) =>
        MovesOn(media.EventDate, media.ArchiveAfterDays) is { } day
        && today.Date >= day
        && !string.Equals(media.Category, ArchiveTab, StringComparison.OrdinalIgnoreCase);

    private static readonly string[] Formats =
        ["d/M/yyyy", "d-M-yyyy", "d.M.yyyy", "d/M/yy", "d-M-yy", "d.M.yy", "yyyy-M-d"];

    /// <summary>Reads a date the way it is written here (17/10/2026, 17-10-26, 17.10.26). False for anything else, and for an empty text.</summary>
    public static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParseExact(text?.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static string Format(DateTime date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
