using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class NamePlateTests
{
    [Fact]
    public void A_plate_needs_a_name_or_a_role()
    {
        Assert.Null(NamePlate.Clean("  ", null));
        Assert.Equal(new NamePlate("Juan Pérez", "Predicación"), NamePlate.Clean(" Juan Pérez ", " Predicación "));
        Assert.Equal("Juan Pérez · Predicación", new NamePlate("Juan Pérez", "Predicación").ToString());
        Assert.Equal("Coro de jóvenes", new NamePlate("Coro de jóvenes").ToString());
    }

    [Fact]
    public void The_list_is_saved_and_read_back()
    {
        var list = new[] { new NamePlate("Juan Pérez", "Pastor"), new NamePlate("Ana Gómez", "Alabanza") };

        Assert.Equal(list, NamePlateList.Parse(NamePlateList.ToSetting(list)));
    }

    [Fact]
    public void A_plate_saved_again_is_not_repeated_and_goes_to_the_top()
    {
        IReadOnlyList<NamePlate> list = [new("Juan Pérez", "Pastor"), new("Ana Gómez", "Alabanza")];

        list = NamePlateList.Saving(list, new NamePlate("ana gómez", "alabanza"));

        Assert.Equal(2, list.Count);
        Assert.Equal("ana gómez", list[0].Name);
    }

    [Fact]
    public void A_plate_can_be_removed()
    {
        IReadOnlyList<NamePlate> list = [new("Juan Pérez", "Pastor"), new("Ana Gómez", "Alabanza")];

        Assert.Equal([new NamePlate("Ana Gómez", "Alabanza")], NamePlateList.Removing(list, new NamePlate("Juan Pérez", "Pastor")));
    }

    [Fact]
    public void A_broken_setting_gives_an_empty_list()
    {
        Assert.Empty(NamePlateList.Parse("no es json"));
        Assert.Empty(NamePlateList.Parse(null));
        Assert.Single(NamePlateList.Parse("""[{"Name":"","Role":""},{"Name":"Juan","Role":""}]"""));
    }

    [Fact]
    public void Showing_and_hiding_a_plate_tells_the_screens()
    {
        var service = new PresentationService();
        var changes = 0;
        service.Changed += (_, _) => changes++;

        service.ShowNamePlate(new NamePlate("Juan Pérez", "Pastor"));
        Assert.Equal("Juan Pérez", service.NamePlate!.Name);

        service.HideNamePlate();
        Assert.Null(service.NamePlate);
        service.HideNamePlate(); // nothing on: nothing to tell

        Assert.Equal(2, changes);
    }
}
