using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class SlideSplitterTests
{
    private const string Chorus =
        "¡Oh Señor!\nquiero que ardas en mi ser.\nComo la zarza\nquiero arder con tu poder,\na los perdidos ganaré como señal";

    [Fact]
    public void Without_a_limit_the_block_comes_back_whole()
    {
        Assert.Equal([Chorus], SlideSplitter.Split(Chorus, 0));
    }

    [Fact]
    public void Five_lines_at_two_a_slide_are_two_two_and_one()
    {
        var pieces = SlideSplitter.Split(Chorus, 2);

        Assert.Equal(
            [
                "¡Oh Señor!\nquiero que ardas en mi ser.",
                "Como la zarza\nquiero arder con tu poder,",
                "a los perdidos ganaré como señal",
            ],
            pieces);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 3)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    public void The_number_of_slides_follows_the_limit(int maxLines, int expected)
    {
        Assert.Equal(expected, SlideSplitter.Split(Chorus, maxLines).Count);
    }

    [Fact]
    public void A_block_that_fits_is_returned_untouched()
    {
        const string two = "Ardiendo en fuego mi alma está,\nardiendo en fuego mi alma está.";

        Assert.Equal([two], SlideSplitter.Split(two, 2));
        Assert.Equal([two], SlideSplitter.Split(two, 4));
    }

    [Fact]
    public void Trailing_blank_lines_do_not_count_or_make_an_empty_slide()
    {
        var pieces = SlideSplitter.Split("Uno\nDos\nTres\n\n", 2);

        Assert.Equal(["Uno\nDos", "Tres"], pieces);
    }

    [Fact]
    public void Windows_line_endings_are_handled()
    {
        Assert.Equal(["Uno\nDos", "Tres"], SlideSplitter.Split("Uno\r\nDos\r\nTres", 2));
    }

    [Fact]
    public void Joining_the_pieces_gives_back_every_line()
    {
        var pieces = SlideSplitter.Split(Chorus, 3);

        Assert.Equal(Chorus, string.Join('\n', pieces));
    }

    [Fact]
    public void Empty_text_comes_back_as_it_is()
    {
        Assert.Equal([""], SlideSplitter.Split("", 2));
    }

    [Fact]
    public void A_limit_over_four_is_held_at_four()
    {
        var eight = string.Join('\n', Enumerable.Range(1, 8).Select(i => $"L{i}"));

        Assert.Equal(2, SlideSplitter.Split(eight, 9).Count);
    }
}
