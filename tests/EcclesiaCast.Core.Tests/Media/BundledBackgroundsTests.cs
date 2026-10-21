using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class BundledBackgroundsTests
{
    [Fact]
    public void On_a_fresh_library_every_background_is_pending_in_alphabetical_order()
    {
        var pending = BundledBackgrounds.Pending(["Tres cruces.jpg", "Amanecer.jpg", "Cálido.jpg"], null);

        Assert.Equal(["Amanecer.jpg", "Cálido.jpg", "Tres cruces.jpg"], pending);
    }

    [Fact]
    public void A_background_brought_in_before_is_not_brought_in_again()
    {
        var setting = BundledBackgrounds.Remember(null, ["Amanecer.jpg"]);

        var pending = BundledBackgrounds.Pending(["Amanecer.jpg", "Aurora.jpg"], setting);

        Assert.Equal(["Aurora.jpg"], pending);
    }

    [Fact]
    public void The_remembered_names_ignore_case()
    {
        var setting = BundledBackgrounds.Remember(null, ["amanecer.JPG"]);

        Assert.Empty(BundledBackgrounds.Pending(["Amanecer.jpg"], setting));
    }

    [Fact]
    public void Only_pictures_are_brought_in()
    {
        var pending = BundledBackgrounds.Pending(["Amanecer.jpg", "LICENCIA.md", "desktop.ini"], null);

        Assert.Equal(["Amanecer.jpg"], pending);
    }

    [Fact]
    public void Remembering_twice_keeps_one_of_each()
    {
        var once = BundledBackgrounds.Remember(null, ["Amanecer.jpg", "Aurora.jpg"]);
        var twice = BundledBackgrounds.Remember(once, ["AURORA.jpg", "Niebla.jpg"]);

        Assert.Equal("Amanecer.jpg|Aurora.jpg|Niebla.jpg", twice);
    }

    [Fact]
    public void A_bar_in_a_name_cannot_split_the_setting()
    {
        var setting = BundledBackgrounds.Remember(null, ["Luz|sombra.jpg"]);

        Assert.Empty(BundledBackgrounds.Pending(["Luz sombra.jpg"], setting));
    }

    [Fact]
    public void The_name_in_the_library_is_the_file_name_without_the_extension()
    {
        Assert.Equal("Noche estrellada", BundledBackgrounds.DisplayName("Noche estrellada.jpg"));
    }
}
