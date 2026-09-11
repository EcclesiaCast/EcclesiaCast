using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Tests.Songs;

public class SlideTextBoxTests
{
    [Fact]
    public void A_slide_with_no_extra_boxes_stores_nothing()
    {
        var section = new SongSection { Text = "Cristo vive" };

        section.SetOverride(new SlideOverride());

        Assert.Null(section.StyleJson);
        Assert.Null(section.GetOverride());
    }

    [Fact]
    public void An_empty_list_of_boxes_is_the_same_as_none()
    {
        var section = new SongSection { Text = "Cristo vive" };

        section.SetOverride(new SlideOverride(Boxes: []));

        Assert.Null(section.StyleJson);
    }

    [Fact]
    public void Extra_boxes_survive_a_round_trip_through_the_library()
    {
        var section = new SongSection { Text = "Cristo vive" };
        var boxes = new List<SlideTextBox>
        {
            new("Serie: El Sermón del Monte", 80, 60, 700, 120, FontSize: 40, AlignH: HAlign.Left),
            SlideTextBox.Fresh("Mateo 5:1-12"),
        };

        section.SetOverride(new SlideOverride(FontSize: 92, Boxes: boxes));
        var read = section.GetOverride();

        Assert.NotNull(read);
        Assert.Equal(92, read!.FontSize);
        Assert.Equal(2, read.TextBoxes.Count);
        Assert.Equal("Serie: El Sermón del Monte", read.TextBoxes[0].Text);
        Assert.Equal(HAlign.Left, read.TextBoxes[0].AlignH);
        Assert.Equal(40, read.TextBoxes[0].FontSize);
        Assert.Equal("Mateo 5:1-12", read.TextBoxes[1].Text);
    }

    [Fact]
    public void A_slide_whose_only_change_is_a_box_still_gets_stored()
    {
        var section = new SongSection { Text = "Cristo vive" };

        section.SetOverride(new SlideOverride(Boxes: [SlideTextBox.Fresh("Coro ×2")]));

        Assert.NotNull(section.StyleJson);
        Assert.Equal("Coro ×2", section.GetOverride()!.TextBoxes[0].Text);
    }

    [Fact]
    public void A_library_written_by_an_older_version_reads_back_without_boxes()
    {
        // Everything saved before this feature existed has no "Boxes" field.
        var section = new SongSection
        {
            Text = "Cristo vive",
            StyleJson = """{"BoxX":110,"BoxY":80,"BoxWidth":1700,"BoxHeight":920,"FontSize":92}""",
        };

        var read = section.GetOverride();

        Assert.NotNull(read);
        Assert.Equal(110, read!.BoxX);
        Assert.Empty(read.TextBoxes);
    }
}
