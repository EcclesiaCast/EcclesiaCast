using System.Globalization;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Updates;

/// <summary>
/// When the program looks for a new version, and what it remembers about it.
/// It asks GitHub at most once a day, a few seconds after opening, and never
/// when the operator turned it off or when this is a development build.
/// </summary>
public static class UpdateSchedule
{
    /// <summary>Setting: "0" when «Buscar actualizaciones al abrir» is off; on otherwise.</summary>
    public const string EnabledKey = "updates.check";

    /// <summary>Setting: the day (yyyy-MM-dd) GitHub last answered.</summary>
    public const string LastCheckKey = "updates.last-check";

    /// <summary>Setting: the version the operator chose «Saltar esta versión» for.</summary>
    public const string SkippedKey = "updates.skipped";

    /// <summary>How long after opening the program the search starts, so the start-up is not slowed.</summary>
    public static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(15);

    public static bool IsEnabled(ISettingsStore settings) => settings.Get(EnabledKey) != "0";

    public static void SetEnabled(ISettingsStore settings, bool enabled) =>
        settings.Set(EnabledKey, enabled ? "1" : "0");

    /// <summary>
    /// True when the program should look on its own now. A day is a calendar
    /// day, not 24 hours: the first time it is opened each day it asks, and
    /// opening it again that same evening does not.
    /// </summary>
    public static bool ShouldCheck(ISettingsStore settings, DateTimeOffset now, bool isDevelopmentBuild)
    {
        if (isDevelopmentBuild || !IsEnabled(settings))
            return false;

        return LastCheckDay(settings) != DayOf(now);
    }

    /// <summary>
    /// Notes that GitHub answered today. It is only called after an answer: a
    /// day without internet must not use up the day's search.
    /// </summary>
    public static void MarkChecked(ISettingsStore settings, DateTimeOffset now) =>
        settings.Set(LastCheckKey, DayOf(now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    /// <summary>The version to leave alone, or null when nothing was skipped.</summary>
    public static AppVersion? Skipped(ISettingsStore settings) => AppVersion.TryParse(settings.Get(SkippedKey));

    public static void Skip(ISettingsStore settings, AppVersion version) =>
        settings.Set(SkippedKey, version.ToString());

    /// <summary>The day on the clock the moment was read from (the program passes the local time).</summary>
    private static DateOnly DayOf(DateTimeOffset moment) => DateOnly.FromDateTime(moment.DateTime);

    private static DateOnly? LastCheckDay(ISettingsStore settings) =>
        DateOnly.TryParseExact(settings.Get(LastCheckKey), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var day)
            ? day
            : null;
}
