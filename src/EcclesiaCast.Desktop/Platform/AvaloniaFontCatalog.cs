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

    /// <summary>
    /// The face drawn for <paramref name="family"/> at <paramref name="weight"/>,
    /// chosen as WPF does (<see cref="FontFaceChoice"/>) among the faces the font
    /// really has. Avalonia is always asked for a face it has loaded as it is,
    /// so the answer does not depend on what was drawn before.
    /// </summary>
    public AvaloniaFace Face(string? family, int weight, bool italic)
    {
        var font = Resolve(family);
        var style = italic ? FontStyle.Italic : FontStyle.Normal;
        var faces = RealFaces(font);
        var choice = FontFaceChoice.Choose(faces.Keys, weight);
        var stretch = faces.TryGetValue(choice.Weight, out var s) ? s : FontStretch.Normal;
        // A thickened face is asked for as Semibold: with the real one already
        // loaded, Avalonia thickens it (it does not, asked for Bold, on Windows).
        var asked = choice.SimulatedBold ? FontWeight.SemiBold : (FontWeight)choice.Weight;
        return new AvaloniaFace(font, asked, style, stretch, choice.SimulatedBold);
    }

    private readonly Dictionary<FontFamily, Dictionary<int, FontStretch>> _faces = [];
    private FontManager? _facesManager;

    /// <summary>
    /// The upright faces <paramref name="font"/> really has (not the ones the
    /// system thickens), by weight, with their width; loading each one here is
    /// also what makes the later answers always the same.
    /// </summary>
    private Dictionary<int, FontStretch> RealFaces(FontFamily font)
    {
        lock (_faces)
        {
            // The loading only holds for the font manager that did it (the
            // tests start a new one for each test).
            if (!ReferenceEquals(_facesManager, FontManager.Current))
            {
                _faces.Clear();
                _facesManager = FontManager.Current;
            }

            if (_faces.TryGetValue(font, out var known))
                return known;

            var faces = new Dictionary<int, FontStretch>();
            IReadOnlyList<Typeface> listed;
            try
            {
                listed = font.FamilyTypefaces;
            }
            catch (InvalidOperationException)
            {
                listed = [];
            }

            foreach (var typeface in listed.Where(t => t.Style == FontStyle.Normal))
            {
                if (!FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphs)
                    || glyphs.Weight != typeface.Weight
                    || glyphs.FontSimulations != FontSimulations.None
                    || glyphs.PlatformTypeface.FontSimulations != FontSimulations.None)
                    continue;

                var weight = (int)typeface.Weight;
                // Of two widths of the same weight (Arial and Arial Narrow), the normal one.
                if (!faces.TryGetValue(weight, out var other)
                    || Math.Abs(typeface.Stretch - FontStretch.Normal) < Math.Abs(other - FontStretch.Normal))
                    faces[weight] = typeface.Stretch;
            }

            _faces[font] = faces;
            return faces;
        }
    }

    /// <summary>The name of a bundled font as its folder spells it.</summary>
    private static string Bundled(string name) =>
        FontSubstitutes.Bundled.First(b => b.Equals(name, StringComparison.OrdinalIgnoreCase));
}
