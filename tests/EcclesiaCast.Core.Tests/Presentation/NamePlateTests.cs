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
    public void The_picture_the_side_and_the_style_are_kept_in_the_list()
    {
        var plate = new NamePlate("Juan Pérez", "Pastor", @"C:\Logos\iglesia.mp4", ImageRight: true, "Prédica");

        var back = Assert.Single(NamePlateList.Parse(NamePlateList.ToSetting([plate])));

        Assert.Equal(plate, back);
        Assert.True(back.ImageIsVideo);
    }

    [Fact]
    public void The_list_keeps_only_the_style_name_never_the_style()
    {
        var plate = new NamePlate("Juan", "Pastor", StyleName: "Moderna") { Style = NamePlateStyle.Presets[1] };

        var json = NamePlateList.ToSetting([plate]);

        Assert.Contains("Moderna", json);
        Assert.DoesNotContain("BackgroundColor", json);
        Assert.Null(Assert.Single(NamePlateList.Parse(json)).Style);
    }

    [Fact]
    public void A_list_saved_before_pictures_and_styles_existed_still_reads()
    {
        // The first plates (18/11) had no picture; the next ones carried an «Animation».
        var back = NamePlateList.Parse("""[{"Name":"Juan","Role":"Pastor"},{"Name":"Ana","Animation":2}]""");

        Assert.Equal(new NamePlate("Juan", "Pastor"), back[0]);
        Assert.False(back[0].HasImage);
        Assert.Equal("", back[1].StyleName);
        Assert.Equal(NamePlateStyle.Classic, back[1].Look);
    }

    [Theory]
    [InlineData(@"C:\a\logo.PNG", true, false)]
    [InlineData(@"C:\a\logo.gif", true, false)]
    [InlineData(@"C:\a\loop.mp4", false, true)]
    [InlineData(@"C:\a\nota.txt", false, false)]
    public void Pictures_and_videos_are_told_apart_by_their_extension(string path, bool image, bool video)
    {
        Assert.Equal(image, NamePlateMedia.IsImage(path));
        Assert.Equal(video, NamePlateMedia.IsVideo(path));
        Assert.Equal(image || video, NamePlateMedia.CanCarry(path));
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
