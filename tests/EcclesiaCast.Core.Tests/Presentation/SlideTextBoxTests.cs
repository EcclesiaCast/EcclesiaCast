using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class SlideTextBoxTests
{
    private static SlideTextBox At(double x, double y, double w = 600, double h = 140) =>
        new("x", x, y, w, h);

    [Fact]
    public void The_first_new_box_goes_where_the_old_single_box_always_went()
    {
        var box = SlideTextBox.FreshAvoiding([]);

        Assert.Equal(660, box.X);
        Assert.Equal(820, box.Y);
        Assert.Equal(600, box.Width);
        Assert.Equal(140, box.Height);
    }

    [Fact]
    public void The_text_given_is_kept()
    {
        Assert.Equal("Serie", SlideTextBox.FreshAvoiding([], "Serie").Text);
    }

    [Fact]
    public void A_second_box_goes_to_the_top_centre_instead_of_on_the_first()
    {
        var first = SlideTextBox.FreshAvoiding([]);

        var second = SlideTextBox.FreshAvoiding([first]);

        Assert.Equal((660, 120), (second.X, second.Y));
        Assert.False(second.Overlaps(first));
    }

    [Fact]
    public void The_spots_are_filled_in_order_without_any_two_boxes_overlapping()
    {
        var boxes = new List<SlideTextBox>();
        for (var i = 0; i < 7; i++)
            boxes.Add(SlideTextBox.FreshAvoiding(boxes));

        Assert.Equal(
            [(660d, 820d), (660d, 120d), (60d, 820d), (1260d, 820d), (60d, 120d), (1260d, 120d), (660d, 470d)],
            boxes.Select(b => (b.X, b.Y)).ToArray());
        for (var i = 0; i < boxes.Count; i++)
            for (var j = i + 1; j < boxes.Count; j++)
                Assert.False(boxes[i].Overlaps(boxes[j]), $"{i} y {j} se pisan");
    }

    [Fact]
    public void A_spot_taken_by_a_box_the_operator_moved_there_is_skipped()
    {
        var moved = At(700, 850);

        var box = SlideTextBox.FreshAvoiding([moved]);

        Assert.Equal((660, 120), (box.X, box.Y));
    }

    [Fact]
    public void A_big_box_over_the_middle_of_the_slide_does_not_stop_the_corners_from_being_used()
    {
        // The words of a song cover most of the slide, but the extra boxes only
        // count against each other.
        var wide = At(60, 300, 1800, 400);

        var box = SlideTextBox.FreshAvoiding([wide]);

        Assert.Equal((660, 820), (box.X, box.Y));
    }

    [Fact]
    public void With_every_spot_taken_the_box_cascades_from_the_middle_until_it_finds_room()
    {
        var boxes = new List<SlideTextBox>();
        for (var i = 0; i < 7; i++)
            boxes.Add(SlideTextBox.FreshAvoiding(boxes));

        var eighth = SlideTextBox.FreshAvoiding(boxes);

        // 40 px steps from the middle box until it clears it (y 630 > 610).
        Assert.Equal((820, 630), (eighth.X, eighth.Y));
        Assert.All(boxes, b => Assert.False(eighth.Overlaps(b)));
        Assert.InRange(eighth.X + eighth.Width, 0, 1920);
        Assert.InRange(eighth.Y + eighth.Height, 0, 1080);
    }

    [Fact]
    public void The_cascade_never_leaves_the_canvas_even_when_it_has_no_room_left()
    {
        // A box over everything the cascade can reach.
        var cover = At(0, 0, 1920, 1080);
        var boxes = new List<SlideTextBox>();
        for (var i = 0; i < 8; i++)
            boxes.Add(SlideTextBox.FreshAvoiding([cover, .. boxes]));

        // The cover is ignored by nobody, so every spot is taken from the start.
        Assert.All(boxes, b =>
        {
            Assert.InRange(b.X, 0, 1920 - b.Width);
            Assert.InRange(b.Y, 0, 1080 - b.Height);
        });
    }

    [Fact]
    public void Boxes_that_only_touch_at_an_edge_do_not_overlap()
    {
        Assert.False(At(0, 0, 100, 100).Overlaps(At(100, 0, 100, 100)));
        Assert.False(At(0, 0, 100, 100).Overlaps(At(0, 100, 100, 100)));
        Assert.True(At(0, 0, 100, 100).Overlaps(At(99, 99, 100, 100)));
    }
}
