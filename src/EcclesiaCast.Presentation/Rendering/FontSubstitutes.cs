namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// What font is drawn when a theme asks for one this computer does not have.
/// The themes of the program say «Segoe UI», which a Mac lacks: Selawik,
/// bundled with the program, has its measures, so the lines are cut at the
/// same words. A font that is installed is always used as it is: on a Windows
/// PC with Segoe UI nothing changes.
/// </summary>
public static class FontSubstitutes
{
    /// <summary>The free fonts that come with the program, on every system.</summary>
    public static IReadOnlyList<string> Bundled { get; } = ["Selawik", "Carlito", "Anton"];

    /// <summary>True when <paramref name="family"/> is one of <see cref="Bundled"/>.</summary>
    public static bool IsBundled(string family) =>
        Bundled.Contains(family.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The stand-in for a font that is missing; null when there is none and
    /// the font is left to the system's own fallback.
    /// </summary>
    public static string? For(string family, PlatformSystem system)
    {
        var name = family.Trim();
        if (name.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase))
            return "Selawik";
        if (name.Equals("Calibri", StringComparison.OrdinalIgnoreCase))
            return "Carlito";
        if (name.Equals("Consolas", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Cascadia", StringComparison.OrdinalIgnoreCase))
            return system == PlatformSystem.Mac ? "Menlo" : "Consolas";
        return null;
    }

    /// <summary>
    /// The font to draw for <paramref name="family"/>: itself when it is
    /// installed or bundled, else its stand-in when that one is there, else
    /// itself (the system then picks a font for each letter).
    /// </summary>
    public static string Resolve(string family, PlatformSystem system, Func<string, bool> isInstalled)
    {
        if (isInstalled(family) || IsBundled(family))
            return family;

        return For(family, system) is { } stand
               && !stand.Equals(family, StringComparison.OrdinalIgnoreCase)
               && (isInstalled(stand) || IsBundled(stand))
            ? stand
            : family;
    }
}
