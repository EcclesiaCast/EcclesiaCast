using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Presentation.Tests;

public class FontSubstitutesTests
{
    private static Func<string, bool> Installed(params string[] names) =>
        name => names.Contains(name, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void An_installed_font_is_used_as_it_is()
    {
        Assert.Equal("Segoe UI", FontSubstitutes.Resolve("Segoe UI", PlatformSystem.Windows, Installed("Segoe UI")));
        Assert.Equal("Calibri", FontSubstitutes.Resolve("Calibri", PlatformSystem.Mac, Installed("Calibri")));
    }

    [Fact]
    public void Segoe_ui_without_segoe_ui_is_selawik()
    {
        Assert.Equal("Selawik", FontSubstitutes.Resolve("Segoe UI", PlatformSystem.Mac, Installed("Helvetica")));
    }

    [Fact]
    public void Calibri_without_calibri_is_carlito()
    {
        Assert.Equal("Carlito", FontSubstitutes.Resolve("Calibri", PlatformSystem.Mac, Installed()));
    }

    [Fact]
    public void Consolas_and_cascadia_are_menlo_on_a_mac_and_consolas_on_windows()
    {
        Assert.Equal("Menlo", FontSubstitutes.Resolve("Consolas", PlatformSystem.Mac, Installed("Menlo")));
        Assert.Equal("Menlo", FontSubstitutes.Resolve("Cascadia Code", PlatformSystem.Mac, Installed("Menlo")));
        Assert.Equal("Consolas", FontSubstitutes.Resolve("Cascadia Mono", PlatformSystem.Windows, Installed("Consolas")));
    }

    [Fact]
    public void A_missing_font_without_a_stand_in_is_left_to_the_system()
    {
        Assert.Equal("Papyrus", FontSubstitutes.Resolve("Papyrus", PlatformSystem.Mac, Installed()));
        Assert.Equal("Consolas", FontSubstitutes.Resolve("Consolas", PlatformSystem.Mac, Installed()));
    }

    [Fact]
    public void The_bundled_fonts_are_always_there()
    {
        Assert.Equal("Anton", FontSubstitutes.Resolve("Anton", PlatformSystem.Mac, Installed()));
        Assert.True(FontSubstitutes.IsBundled("selawik"));
        Assert.False(FontSubstitutes.IsBundled("Arial"));
    }
}
