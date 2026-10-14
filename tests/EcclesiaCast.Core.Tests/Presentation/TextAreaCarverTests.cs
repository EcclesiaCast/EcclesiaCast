using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class TextAreaCarverTests
{
    // The usual area: the whole canvas less 120 px of margin all around.
    private static readonly BoxRect Area = new(120, 120, 1680, 840);

    [Fact]
    public void With_no_boxes_the_area_is_left_as_it_was()
    {
        Assert.Equal(Area, TextAreaCarver.Carve(Area, []));
    }

    [Fact]
    public void A_wide_box_under_the_words_ends_the_area_just_above_it()
    {
        var strip = new BoxRect(120, 800, 1680, 140);

        var carved = TextAreaCarver.Carve(Area, [strip]);

        Assert.Equal(120, carved.X);
        Assert.Equal(120, carved.Y);
        Assert.Equal(1680, carved.W);
        Assert.Equal(800 - TextAreaCarver.Gap - 120, carved.H);
        Assert.True(carved.Bottom <= strip.Y);
    }

    [Fact]
    public void A_wide_box_over_the_words_starts_the_area_just_below_it()
    {
        var title = new BoxRect(120, 40, 1680, 140);

        var carved = TextAreaCarver.Carve(Area, [title]);

        Assert.Equal(title.Bottom + TextAreaCarver.Gap, carved.Y);
        Assert.Equal(Area.Bottom, carved.Bottom);
        Assert.Equal(Area.X, carved.X);
        Assert.Equal(Area.W, carved.W);
    }

    [Fact]
    public void A_narrow_box_in_a_corner_takes_nothing_from_the_words()
    {
        var corner = new BoxRect(1500, 800, 300, 140);

        Assert.Equal(Area, TextAreaCarver.Carve(Area, [corner]));
    }

    [Fact]
    public void A_box_that_covers_exactly_half_of_the_width_cuts_and_a_little_less_does_not()
    {
        var half = new BoxRect(120, 800, 840, 140);
        var almost = new BoxRect(120, 800, 839, 140);

        Assert.NotEqual(Area, TextAreaCarver.Carve(Area, [half]));
        Assert.Equal(Area, TextAreaCarver.Carve(Area, [almost]));
    }

    [Fact]
    public void A_wide_box_that_does_not_touch_the_area_changes_nothing()
    {
        var far = new BoxRect(0, 1000, 1920, 80);

        Assert.Equal(Area, TextAreaCarver.Carve(Area, [far]));
    }

    [Fact]
    public void A_box_hanging_half_outside_the_area_counts_only_for_the_width_inside()
    {
        // 1280 px wide but only 80 of them over the area's left edge.
        var mostlyOutside = new BoxRect(-1080, 800, 1280, 140);

        Assert.Equal(Area, TextAreaCarver.Carve(Area, [mostlyOutside]));
    }

    [Fact]
    public void The_words_are_never_squeezed_below_the_minimum_height()
    {
        var onTop = new BoxRect(120, 130, 1680, 800);

        var carved = TextAreaCarver.Carve(Area, [onTop]);

        Assert.True(carved.H >= TextAreaCarver.MinHeight);
    }

    [Fact]
    public void A_box_above_that_leaves_less_than_the_minimum_keeps_the_strip_inside_the_area()
    {
        var almostEverything = new BoxRect(120, 100, 1680, 850);

        var carved = TextAreaCarver.Carve(Area, [almostEverything]);

        Assert.True(carved.H >= TextAreaCarver.MinHeight);
        Assert.Equal(Area.Bottom, carved.Bottom);
    }

    [Fact]
    public void A_title_above_and_a_translation_below_both_cut_in()
    {
        var title = new BoxRect(120, 40, 1680, 100);
        var translation = new BoxRect(120, 840, 1680, 120);

        var carved = TextAreaCarver.Carve(Area, [translation, title]);

        Assert.Equal(title.Bottom + TextAreaCarver.Gap, carved.Y);
        Assert.Equal(translation.Y - TextAreaCarver.Gap, carved.Bottom);
    }

    [Fact]
    public void The_order_the_boxes_come_in_does_not_change_the_result()
    {
        var title = new BoxRect(120, 40, 1680, 100);
        var translation = new BoxRect(120, 840, 1680, 120);

        Assert.Equal(
            TextAreaCarver.Carve(Area, [title, translation]),
            TextAreaCarver.Carve(Area, [translation, title]));
    }
}
