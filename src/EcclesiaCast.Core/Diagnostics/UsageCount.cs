using System.Globalization;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Diagnostics;

/// <summary>
/// What is sent to count how many churches use the program: a number drawn
/// at random once per installation, the day (as this PC sees it) and the
/// version. No name, no church, no song, nothing typed.
/// </summary>
/// <param name="Install">The random number of this installation.</param>
/// <param name="Day">The day something was projected, «2026-11-15».</param>
/// <param name="Version">The program's version.</param>
public sealed record UsagePing(string Install, string Day, string Version);

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

    public static bool IsEnabled(ISettingsStore settings) => settings.Get(EnabledKey) != "0";

    public static void SetEnabled(ISettingsStore settings, bool enabled) =>
        settings.Set(EnabledKey, enabled ? "1" : "0");

    /// <summary>
    /// Whether today's use still has to be told. Never from a build made to
    /// develop the program (it would count the developer), never when it is
    /// off, and once a day.
    /// </summary>
    public static bool ShouldSend(ISettingsStore settings, DateTimeOffset now, bool isDevelopmentBuild) =>
        !isDevelopmentBuild && IsEnabled(settings) && settings.Get(LastSentKey) != Day(now);

    /// <summary>What to send for today. Draws the installation's number the first time.</summary>
    public static UsagePing Ping(ISettingsStore settings, DateTimeOffset now, string version)
    {
        var install = settings.Get(InstallKey);
        if (!IsInstallNumber(install))
        {
            install = Guid.NewGuid().ToString("N");
            settings.Set(InstallKey, install);
        }

        return new UsagePing(install!, Day(now), version);
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
    }

    /// <summary>32 hexadecimal characters, the only shape the service accepts.</summary>
    public static bool IsInstallNumber(string? value) =>
        value is { Length: 32 } && value.All(Uri.IsHexDigit);

    private static string Day(DateTimeOffset now) =>
        now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
