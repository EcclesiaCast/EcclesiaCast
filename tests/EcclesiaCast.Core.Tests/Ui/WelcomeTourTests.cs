using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.Core.Tests.Ui;

public class WelcomeTourTests
{
    private static readonly TourBox Window = new(0, 0, 1280, 760);
    private const double CardW = 360, CardH = 200, Gap = 16;

    [Fact]
    public void Starts_with_a_greeting_ends_with_a_goodbye_and_lights_each_part_once()
    {
        var steps = WelcomeTour.Steps;

        Assert.Null(steps[0].Target);
        Assert.Null(steps[^1].Target);
        var targets = steps.Skip(1).SkipLast(1).Select(s => s.Target).ToList();
        Assert.All(targets, Assert.NotNull);
        Assert.Equal(targets.Count, targets.Distinct().Count());
    }

    [Fact]
    public void Every_card_is_short_enough_to_read_in_a_few_seconds()
    {
        Assert.All(WelcomeTour.Steps, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Title));
            Assert.InRange(s.Text.Length, 20, WelcomeTour.MaxTextLength);
        });
    }

    [Fact]
    public void Parts_not_on_show_are_skipped_both_ways()
    {
        var steps = WelcomeTour.Steps;
        bool NoPlaylist(string t) => t != WelcomeTour.Playlist;
        var bible = Index(WelcomeTour.Bible);
        var slides = Index(WelcomeTour.Slides);

        Assert.Equal(slides, WelcomeTour.Next(steps, bible, 1, NoPlaylist));
        Assert.Equal(bible, WelcomeTour.Next(steps, slides, -1, NoPlaylist));
    }

    [Fact]
    public void Past_the_last_step_or_before_the_first_there_is_none()
    {
        var steps = WelcomeTour.Steps;

        Assert.Equal(-1, WelcomeTour.Next(steps, steps.Count - 1, 1, _ => true));
        Assert.Equal(-1, WelcomeTour.Next(steps, 0, -1, _ => true));
    }

    [Fact]
    public void With_nothing_on_show_the_greeting_leads_straight_to_the_goodbye()
    {
        var steps = WelcomeTour.Steps;

        Assert.Equal(steps.Count - 1, WelcomeTour.Next(steps, 0, 1, _ => false));
    }

    [Fact]
    public void The_lit_part_gets_room_around_it_but_stays_inside_the_window()
    {
        Assert.Equal(new TourBox(92, 92, 216, 116), WelcomeTour.Spot(new TourBox(100, 100, 200, 100), Window, 8));
        Assert.Equal(new TourBox(0, 0, 208, 108), WelcomeTour.Spot(new TourBox(0, 0, 200, 100), Window, 8));
    }

    [Fact]
    public void The_card_goes_to_the_right_of_a_part_on_the_left()
    {
        var (x, y) = WelcomeTour.PlaceCard(new TourBox(10, 100, 300, 500), Window, CardW, CardH, Gap);

        Assert.Equal(326, x);
        Assert.Equal(100, y);
    }

    [Fact]
    public void The_card_goes_to_the_left_of_a_part_on_the_right()
    {
        var (x, _) = WelcomeTour.PlaceCard(new TourBox(900, 80, 370, 600), Window, CardW, CardH, Gap);

        Assert.Equal(900 - Gap - CardW, x);
    }

    [Fact]
    public void A_part_as_wide_as_the_window_gets_the_card_under_or_over_it()
    {
        var under = WelcomeTour.PlaceCard(new TourBox(0, 40, 1280, 60), Window, CardW, CardH, Gap);
        var over = WelcomeTour.PlaceCard(new TourBox(0, 500, 1280, 250), Window, CardW, CardH, Gap);

        Assert.Equal(40 + 60 + Gap, under.Y);
        Assert.Equal(500 - Gap - CardH, over.Y);
    }

    [Fact]
    public void The_card_never_hangs_off_the_bottom_of_the_window()
    {
        var (_, y) = WelcomeTour.PlaceCard(new TourBox(10, 700, 300, 50), Window, CardW, CardH, Gap);

        Assert.True(y + CardH <= Window.Bottom);
    }

    [Fact]
    public void With_nothing_lit_the_card_is_in_the_middle()
    {
        var (x, y) = WelcomeTour.PlaceCard(default, Window, CardW, CardH, Gap);

        Assert.Equal((1280 - CardW) / 2, x);
        Assert.Equal((760 - CardH) / 2, y);
    }

    [Fact]
    public void A_part_that_fills_the_window_gets_the_card_over_its_lower_part()
    {
        var (_, y) = WelcomeTour.PlaceCard(Window, Window, CardW, CardH, Gap);

        Assert.Equal(760 - CardH - 2 * Gap, y);
    }

    private static int Index(string target) =>
        WelcomeTour.Steps.Select((s, i) => (s, i)).First(p => p.s.Target == target).i;
}
