using System.Globalization;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Diagnostics;

/// <summary>
/// What is sent to count how many churches use the program: a number drawn
/// at random once per installation, the day (as this PC sees it) and the
/// version, and the name of the church only if the person typed it in Ajustes
/// (optional, informative). No song, nothing else typed.
/// </summary>
/// <param name="Install">The random number of this installation.</param>
/// <param name="Day">The day something was projected, «2026-11-15».</param>
/// <param name="Version">The program's version.</param>
public sealed record UsagePing(string Install, string Day, string Version, string Church = "");

/// <summary>
/// What kind of copy of the program is running, for the count of use. Only a
/// copy a church really uses counts: a released version, installed by the
/// installer, on its own library. Copies made to try things out would count
/// the people who make the program, not the churches.
/// </summary>
/// <param name="IsDevelopmentBuild">A Debug build, or one whose version can not be read.</param>
/// <param name="IsPreRelease">A test version: «1.4.0-prueba.4», «1.5.0-beta.1» (anything with «-»).</param>
/// <param name="UsesTestLibrary">Pointed at a copy of a library (ECCLESIACAST_DATA_DIR), as when trying it on someone's PC.</param>
/// <param name="IsInstalled">
/// Runs from where the installer put it. The installer always leaves its
/// uninstaller («unins000.exe») next to the program; a copy run from a loose
/// folder (a build, an unzipped test) has none.
/// </param>
public sealed record UsageCopy(bool IsDevelopmentBuild, bool IsPreRelease, bool UsesTestLibrary, bool IsInstalled)
{
    /// <summary>Name of the uninstaller Inno Setup leaves next to the program.</summary>
    public const string UninstallerFile = "unins000.exe";

    public bool Counts => !IsDevelopmentBuild && !IsPreRelease && !UsesTestLibrary && IsInstalled;

    /// <param name="version">The program's version, as it reports it («1.5.0», «1.5.0-prueba.2+abc123»).</param>
    /// <param name="testLibrary">The value of ECCLESIACAST_DATA_DIR, if any.</param>
    /// <param name="uninstallerNextToProgram">Whether <see cref="UninstallerFile"/> is in the program's folder.</param>
    public static UsageCopy Of(string? version, bool isDevelopmentBuild, string? testLibrary, bool uninstallerNextToProgram) =>
        new(isDevelopmentBuild,
            IsPreReleaseVersion(version),
            !string.IsNullOrWhiteSpace(testLibrary),
            uninstallerNextToProgram);

    /// <summary>A version with a «-» before its build data is a test one; an unreadable one counts as a test too.</summary>
    public static bool IsPreReleaseVersion(string? version)
    {
        var plain = (version ?? string.Empty).Split('+')[0].Trim();
        return plain.Length == 0 || plain.Contains('-');
    }
}

/// <summary>
/// The anonymous count of use (Ajustes → Actualizaciones, on unless turned
/// off). The first time something is projected each day, the program says
/// «this installation was used today». Counting on projecting rather than on
/// opening leaves out whoever only opened it to look around.
/// </summary>
public static class UsageCount
{
    public const string EnabledKey = "usage.count";
    public const string InstallKey = "usage.install";
    public const string LastSentKey = "usage.last-sent";
    public const string ChurchKey = "usage.church";
    public const int MaxChurchLength = 60;

    public static bool IsEnabled(ISettingsStore settings) => settings.Get(EnabledKey) != "0";

    public static void SetEnabled(ISettingsStore settings, bool enabled) =>
        settings.Set(EnabledKey, enabled ? "1" : "0");

    /// <summary>
    /// Whether today's use still has to be told. Only from a copy a church
    /// really uses (see <see cref="UsageCopy"/>), never when it is off, and
    /// once a day.
    /// </summary>
    public static bool ShouldSend(ISettingsStore settings, DateTimeOffset now, UsageCopy copy) =>
        copy.Counts && IsEnabled(settings) && settings.Get(LastSentKey) != Day(now);

    /// <summary>
    /// The same, for a copy that is installed, released and on its own
    /// library: only the build kind decides.
    /// </summary>
    public static bool ShouldSend(ISettingsStore settings, DateTimeOffset now, bool isDevelopmentBuild) =>
        ShouldSend(settings, now, new UsageCopy(isDevelopmentBuild, IsPreRelease: false, UsesTestLibrary: false, IsInstalled: true));

    /// <summary>What to send for today. Draws the installation's number the first time.</summary>
    public static UsagePing Ping(ISettingsStore settings, DateTimeOffset now, string version)
    {
        var install = settings.Get(InstallKey);
        if (!IsInstallNumber(install))
        {
            install = Guid.NewGuid().ToString("N");
            settings.Set(InstallKey, install);
        }

        return new UsagePing(install!, Day(now), version, ChurchName(settings));
    }

    /// <summary>The name of the church the person chose to tell, cleaned up; empty if none.</summary>
    public static string ChurchName(ISettingsStore settings) => CleanChurch(settings.Get(ChurchKey));

    public static string CleanChurch(string? value)
    {
        var text = string.Join(" ", (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length <= MaxChurchLength ? text : text[..MaxChurchLength].TrimEnd();
    }

    /// <summary>Saves the name and asks for today to be told again, so it arrives now and not tomorrow.</summary>
    public static void SetChurchName(ISettingsStore settings, string? value)
    {
        var clean = CleanChurch(value);
        if (clean == ChurchName(settings))
            return;

        settings.Set(ChurchKey, clean);
        settings.Set(LastSentKey, string.Empty);
    }

    /// <summary>Remembers that today was told, so it is not told again. Only after it got there.</summary>
    public static void MarkSent(ISettingsStore settings, DateTimeOffset now) => settings.Set(LastSentKey, Day(now));

    /// <summary>
    /// Turning the count off also forgets the number: if it is turned on
    /// again, it counts as a new installation and nothing ties the two.
    /// </summary>
    public static void Forget(ISettingsStore settings)
    {
        settings.Set(InstallKey, string.Empty);
        settings.Set(LastSentKey, string.Empty);
        settings.Set(ChurchKey, string.Empty);
    }

    /// <summary>32 hexadecimal characters, the only shape the service accepts.</summary>
    public static bool IsInstallNumber(string? value) =>
        value is { Length: 32 } && value.All(Uri.IsHexDigit);

    private static string Day(DateTimeOffset now) =>
        now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
