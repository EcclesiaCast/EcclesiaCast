using Avalonia.Media;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>
/// The fonts of the Avalonia interface: the ones installed on the computer
/// plus the free ones that come with the program (marked as such), and which
/// font is really drawn for a name a theme asks for
/// (<see cref="FontSubstitutes"/>: Segoe UI → Selawik where it is missing).
/// </summary>
public sealed class AvaloniaFontCatalog : IFontCatalog
{
    private const string FontsFolder = "avares://EcclesiaCast.Desktop/Assets/Fonts/";

    /// <summary>The catalog of this computer.</summary>
    public static AvaloniaFontCatalog Current => _current ??= new AvaloniaFontCatalog(
        () => FontManager.Current.SystemFonts.Select(f => f.Name),
        OperatingSystem.IsMacOS() ? PlatformSystem.Mac : PlatformSystem.Windows);

    private static AvaloniaFontCatalog? _current;

    private readonly Func<IEnumerable<string>> _systemFonts;
    private readonly PlatformSystem _system;
    private HashSet<string>? _installed;
    private readonly Dictionary<string, FontFamily> _families = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="systemFonts">The names of the fonts installed on the computer.</param>
    /// <param name="system">The system, which decides some stand-ins (Consolas → Menlo on a Mac).</param>
    public AvaloniaFontCatalog(Func<IEnumerable<string>> systemFonts, PlatformSystem system)
    {
        _systemFonts = systemFonts;
        _system = system;
    }

    private HashSet<string> Installed =>
        _installed ??= new HashSet<string>(_systemFonts(), StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> InstalledFamilies() =>
        Installed.Concat(FontSubstitutes.Bundled)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

    public bool IsBundled(string family) => FontSubstitutes.IsBundled(family) && !Installed.Contains(family);

    /// <summary>The name of the font drawn for <paramref name="family"/> on this computer.</summary>
    public string ResolveName(string? family) =>
        FontSubstitutes.Resolve(string.IsNullOrWhiteSpace(family) ? "Segoe UI" : family, _system, Installed.Contains);

    /// <summary>
    /// The font drawn for <paramref name="family"/>: the installed one, or the
    /// one bundled with the program when that is what is there.
    /// </summary>
    public FontFamily Resolve(string? family)
    {
        var name = ResolveName(family);
        lock (_families)
        {
            if (!_families.TryGetValue(name, out var font))
            {
                font = IsBundled(name)
                    ? new FontFamily(FontsFolder + Bundled(name) + "#" + Bundled(name))
                    : new FontFamily(name);
                _families[name] = font;
            }
            return font;
        }
    }

    /// <summary>The name of a bundled font as its folder spells it.</summary>
    private static string Bundled(string name) =>
        FontSubstitutes.Bundled.First(b => b.Equals(name, StringComparison.OrdinalIgnoreCase));
}
