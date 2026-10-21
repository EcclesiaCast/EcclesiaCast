using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class BundledBackgroundsTests
{
    [Fact]
    public void On_a_fresh_library_every_background_is_pending_in_alphabetical_order()
    {
        var pending = BundledBackgrounds.Pending(["Tres cruces.mp4", "Amanecer.mp4", "Cálido.mp4"], null);

        Assert.Equal(["Amanecer.mp4", "Cálido.mp4", "Tres cruces.mp4"], pending);
    }

    [Fact]
    public void A_background_brought_in_before_is_not_brought_in_again()
    {
        var setting = BundledBackgrounds.Remember(null, ["Amanecer.mp4"]);

        var pending = BundledBackgrounds.Pending(["Amanecer.mp4", "Aurora.mp4"], setting);

        Assert.Equal(["Aurora.mp4"], pending);
    }

    [Fact]
    public void The_remembered_names_ignore_case()
    {
        var setting = BundledBackgrounds.Remember(null, ["amanecer.MP4"]);

        Assert.Empty(BundledBackgrounds.Pending(["Amanecer.mp4"], setting));
    }

    [Fact]
    public void Only_videos_are_brought_in()
    {
        var pending = BundledBackgrounds.Pending(["Amanecer.mp4", "Amanecer.jpg", "LICENCIA.md", "desktop.ini"], null);

        Assert.Equal(["Amanecer.mp4"], pending);
    }

    [Fact]
    public void Remembering_twice_keeps_one_of_each()
    {
        var once = BundledBackgrounds.Remember(null, ["Amanecer.mp4", "Aurora.mp4"]);
        var twice = BundledBackgrounds.Remember(once, ["AURORA.mp4", "Niebla.mp4"]);

        Assert.Equal("Amanecer.mp4|Aurora.mp4|Niebla.mp4", twice);
    }

    [Fact]
    public void A_bar_in_a_name_cannot_split_the_setting()
    {
        var setting = BundledBackgrounds.Remember(null, ["Luz|sombra.mp4"]);

        Assert.Empty(BundledBackgrounds.Pending(["Luz sombra.mp4"], setting));
    }

    [Fact]
    public void The_name_in_the_library_is_the_file_name_without_the_extension()
    {
        Assert.Equal("Noche estrellada", BundledBackgrounds.DisplayName("Noche estrellada.mp4"));
    }
}
