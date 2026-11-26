using Avalonia;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Desktop.Platform;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The screens as the Avalonia program lists them, without a real screen.</summary>
public sealed class DisplayProviderTests
{
    [Fact]
    public void Each_screen_keeps_its_pixels_and_its_scale()
    {
        var displays = AvaloniaDisplayProvider.ToDisplays(
        [
            new ScreenData("Pantalla integrada", new PixelRect(0, 0, 2880, 1800), 2, IsPrimary: true),
            new ScreenData("Proyector", new PixelRect(2880, 0, 1920, 1080), 1, IsPrimary: false),
        ]);

        Assert.Equal(
            [
                new DisplayInfo("Pantalla integrada", 0, 0, 2880, 1800, true, 2),
                new DisplayInfo("Proyector", 2880, 0, 1920, 1080, false, 1),
            ],
            displays);
    }

    [Fact]
    public void Two_screens_with_the_same_name_can_be_told_apart()
    {
        var displays = AvaloniaDisplayProvider.ToDisplays(
        [
            new ScreenData("DELL U2415", new PixelRect(0, 0, 1920, 1200), 1, true),
            new ScreenData("DELL U2415", new PixelRect(1920, 0, 1920, 1200), 1, false),
            new ScreenData(null, new PixelRect(3840, 0, 1280, 720), 0, false),
        ]);

        Assert.Equal(["DELL U2415", "DELL U2415 (2)", "Pantalla"], displays.Select(d => d.DeviceName));
        Assert.Equal(1, displays[2].Scale);
    }

    [Fact]
    public void A_screen_is_found_again_by_its_name_even_in_another_mode()
    {
        var projector = new DisplayInfo("Proyector", 1920, 0, 1920, 1080, false);
        var back = new DisplayInfo("Proyector", 1440, 0, 1280, 720, false);
        var mac = new DisplayInfo("Pantalla integrada", 0, 0, 1440, 900, true, 2);

        Assert.Equal(back, AvaloniaDisplayProvider.Find(projector, [mac, back]));
        Assert.Null(AvaloniaDisplayProvider.Find(projector, [mac]));
    }

    [Fact]
    public void Without_a_window_there_are_no_screens_yet()
    {
        Assert.Empty(new AvaloniaDisplayProvider().GetDisplays());
    }
}
