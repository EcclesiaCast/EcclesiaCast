using Avalonia.Headless.XUnit;
using Avalonia.Media;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Desktop.Tests;

public sealed class FontCatalogTests
{
    /// <summary>A Mac as it comes: no Segoe UI, no Calibri.</summary>
    private static AvaloniaFontCatalog Mac() =>
        new(() => ["Helvetica", "Arial", "Menlo", "Times New Roman"], PlatformSystem.Mac);

    private static string DrawnFamily(FontFamily family, FontWeight weight = FontWeight.Normal)
    {
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, weight), out var glyphs));
        return glyphs!.FamilyName;
    }

    [AvaloniaFact]
    public void Segoe_ui_on_a_computer_without_it_is_drawn_with_the_bundled_selawik()
    {
        var family = Mac().Resolve("Segoe UI");

        Assert.Equal("Selawik", DrawnFamily(family));
    }

    [AvaloniaFact]
    public void The_bold_of_a_theme_finds_the_semibold_selawik()
    {
        var family = Mac().Resolve("Segoe UI");

        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, FontWeight.SemiBold), out var glyphs));
        Assert.Equal(FontWeight.SemiBold, glyphs!.Weight);
    }

    [AvaloniaFact]
    public void Calibri_on_a_mac_is_carlito_and_anton_is_always_there()
    {
        var catalog = Mac();

        Assert.Equal("Carlito", DrawnFamily(catalog.Resolve("Calibri")));
        Assert.Equal("Anton", DrawnFamily(catalog.Resolve("Anton")));
        Assert.Equal("Menlo", catalog.ResolveName("Consolas"));
    }

    [AvaloniaFact]
    public void The_list_has_the_installed_fonts_and_marks_the_bundled_ones()
    {
        var catalog = Mac();

        var all = catalog.InstalledFamilies();

        Assert.Contains("Helvetica", all);
        Assert.Contains("Selawik", all);
        Assert.Contains("Carlito", all);
        Assert.Contains("Anton", all);
        Assert.True(catalog.IsBundled("Selawik"));
        Assert.False(catalog.IsBundled("Helvetica"));
    }

    [AvaloniaFact]
    public void An_installed_font_is_drawn_as_it_is()
    {
        var catalog = new AvaloniaFontCatalog(() => ["Segoe UI"], PlatformSystem.Windows);

        Assert.Equal("Segoe UI", catalog.ResolveName("Segoe UI"));
    }
}
