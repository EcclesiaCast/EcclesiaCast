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
        if (!faces.TryGetValue(choice.Weight, out var face))
            return new AvaloniaFace(font, (FontWeight)choice.Weight, style, FontStretch.Normal, false);

        if (!choice.SimulatedBold)
            return new AvaloniaFace(font, face.Asked, style, face.Stretch, false);

        // The thickened face is made from the file of the real one, asked for
        // as Semibold in a collection of its own: the system's own answer
        // depends on what it thinks is bold (a Mac takes Impact for bold and
        // never thickens it).
        return new AvaloniaFace(Thickened(font, face, italic), FontWeight.SemiBold, style, face.Stretch, true);
    }

    /// <summary>A face the font really has: what to ask the system for it, and its width.</summary>
    private sealed record RealFace(FontWeight Asked, FontStretch Stretch, GlyphTypeface Glyphs);

    private readonly Dictionary<FontFamily, Dictionary<int, RealFace>> _faces = [];
    private readonly Dictionary<FontFamily, FontFamily> _thickened = [];
    private ThickenedFonts? _thickenedFonts;
    private FontManager? _facesManager;

    /// <summary>The address of the collection of the faces to thicken.</summary>
    private static readonly Uri ThickenedKey = new("fonts:EcclesiaCastEngrosadas");

    /// <summary>
    /// <paramref name="font"/> as a font of its own made from the file of
    /// <paramref name="face"/> (and of its italic, if it has a real one), so
    /// that Avalonia, asked for a heavier weight, thickens it as WPF does: from
    /// the weight the file says it has.
    /// </summary>
    private FontFamily Thickened(FontFamily font, RealFace face, bool italic)
    {
        lock (_faces)
        {
            if (_thickened.TryGetValue(font, out var known))
                return known;

            if (_thickenedFonts is null)
            {
                _thickenedFonts = new ThickenedFonts(ThickenedKey);
                FontManager.Current.AddFontCollection(_thickenedFonts);
            }

            var added = _thickenedFonts.Add(face.Glyphs);
            if (italic
                && FontManager.Current.TryGetGlyphTypeface(new Typeface(font, FontStyle.Italic, face.Asked, face.Stretch), out var slanted)
                && slanted.Style == FontStyle.Italic
                && slanted.FontSimulations == FontSimulations.None
                && slanted.PlatformTypeface.FontSimulations == FontSimulations.None)
                _thickenedFonts.Add(slanted);

            var thickened = added is { } name ? new FontFamily(ThickenedKey + "#" + name) : font;
            _thickened[font] = thickened;
            return thickened;
        }
    }

    /// <summary>
    /// The upright faces <paramref name="font"/> really has (not the ones the
    /// system thickens), by the weight their file says (the one WPF goes by: a
    /// Mac calls Impact bold, its file says Regular), with what to ask the
    /// system for each; loading each one here is also what makes the later
    /// answers always the same.
    /// </summary>
    private Dictionary<int, RealFace> RealFaces(FontFamily font)
    {
        lock (_faces)
        {
            // The loading only holds for the font manager that did it (the
            // tests start a new one for each test).
            if (!ReferenceEquals(_facesManager, FontManager.Current))
            {
                _faces.Clear();
                _thickened.Clear();
                _thickenedFonts = null;
                _facesManager = FontManager.Current;
            }

            if (_faces.TryGetValue(font, out var known))
                return known;

            var faces = new Dictionary<int, RealFace>();
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
                    || glyphs.Style != FontStyle.Normal
                    || glyphs.FontSimulations != FontSimulations.None
                    || glyphs.PlatformTypeface.FontSimulations != FontSimulations.None)
                    continue;

                var weight = (int)glyphs.Weight;
                // Of two widths of the same weight (Arial and Arial Narrow), the normal one.
                if (!faces.TryGetValue(weight, out var other)
                    || Math.Abs(typeface.Stretch - FontStretch.Normal) < Math.Abs(other.Stretch - FontStretch.Normal))
                    faces[weight] = new RealFace(typeface.Weight, typeface.Stretch, glyphs);
            }

            _faces[font] = faces;
            return faces;
        }
    }

    /// <summary>The name of a bundled font as its folder spells it.</summary>
    private static string Bundled(string name) =>
        FontSubstitutes.Bundled.First(b => b.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Copies of real faces, which Avalonia thickens by the weight their file
    /// says when a heavier one is asked for (as it does with the fonts that
    /// come with the program).
    /// </summary>
    private sealed class ThickenedFonts(Uri key) : Avalonia.Media.Fonts.FontCollectionBase
    {
        public override Uri Key => key;

        /// <summary>Adds the file of <paramref name="face"/>; the family name it is under, or null.</summary>
        public string? Add(GlyphTypeface face)
        {
            if (!face.PlatformTypeface.TryGetStream(out var stream))
                return null;
            using (stream)
                return TryAddGlyphTypeface(stream, out var added) ? added.FamilyName : null;
        }
    }
}
