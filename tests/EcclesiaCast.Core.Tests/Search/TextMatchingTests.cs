using EcclesiaCast.Core.Search;

namespace EcclesiaCast.Core.Tests.Search;

public class TextMatchingTests
{
    [Fact]
    public void A_line_break_becomes_a_single_space()
    {
        Assert.Equal("las cadenas de la muerte", TextMatching.CollapseWhitespace("las cadenas\r\nde la muerte"));
    }

    [Fact]
    public void Repeated_spaces_and_tabs_become_one()
    {
        Assert.Equal("a b c", TextMatching.CollapseWhitespace("  a   b\t\tc \n"));
    }

    [Fact]
    public void Nothing_to_collapse_gives_an_empty_text()
    {
        Assert.Equal(string.Empty, TextMatching.CollapseWhitespace(null));
        Assert.Equal(string.Empty, TextMatching.CollapseWhitespace("   \n "));
    }

    [Fact]
    public void The_pattern_wraps_the_collapsed_query_in_wildcards()
    {
        Assert.Equal("%echa sobre%", TextMatching.ContainsPattern("  echa \n sobre "));
    }

    [Fact]
    public void A_blank_query_has_no_pattern()
    {
        Assert.Null(TextMatching.ContainsPattern("  "));
        Assert.Null(TextMatching.ContainsPattern(null));
    }
}
