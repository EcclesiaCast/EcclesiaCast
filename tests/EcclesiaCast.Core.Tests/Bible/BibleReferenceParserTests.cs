using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleReferenceParserTests
{
    [Fact]
    public void Parses_a_single_verse()
    {
        var reference = BibleReferenceParser.TryParse("Juan 3:16");

        Assert.NotNull(reference);
        Assert.Equal(43, reference!.BookNumber);
        Assert.Equal(3, reference.Chapter);
        Assert.Equal(16, reference.VerseStart);
        Assert.Equal(16, reference.VerseEnd);
    }

    [Fact]
    public void Parses_a_short_abbreviation_with_a_range()
    {
        var reference = BibleReferenceParser.TryParse("jn 3:16-18");

        Assert.NotNull(reference);
        Assert.Equal(43, reference!.BookNumber);
        Assert.Equal(16, reference.VerseStart);
        Assert.Equal(18, reference.VerseEnd);
    }

    [Fact]
    public void Parses_a_whole_chapter_without_verse()
    {
        var reference = BibleReferenceParser.TryParse("sal 23");

        Assert.NotNull(reference);
        Assert.Equal(19, reference!.BookNumber);
        Assert.Equal(23, reference.Chapter);
        Assert.Null(reference.VerseStart);
        Assert.Null(reference.VerseEnd);
    }

    [Fact]
    public void Parses_a_book_with_a_leading_number()
    {
        var reference = BibleReferenceParser.TryParse("1 co 13");

        Assert.NotNull(reference);
        Assert.Equal(46, reference!.BookNumber);
        Assert.Equal(13, reference.Chapter);
    }

    [Fact]
    public void Full_name_with_multiple_words_also_parses()
    {
        var reference = BibleReferenceParser.TryParse("1 Corintios 13:4-7");

        Assert.NotNull(reference);
        Assert.Equal(46, reference!.BookNumber);
        Assert.Equal(4, reference.VerseStart);
        Assert.Equal(7, reference.VerseEnd);
    }

    [Theory]
    [InlineData("juan 3 16")]
    [InlineData("juan 3.16")]
    [InlineData("Juan  3  16")]
    public void A_space_or_a_dot_also_separates_the_verse(string input)
    {
        var reference = BibleReferenceParser.TryParse(input);

        Assert.NotNull(reference);
        Assert.Equal(43, reference!.BookNumber);
        Assert.Equal(3, reference.Chapter);
        Assert.Equal(16, reference.VerseStart);
    }

    [Fact]
    public void A_space_separated_verse_works_after_a_numbered_book()
    {
        var reference = BibleReferenceParser.TryParse("1 juan 4 8");

        Assert.NotNull(reference);
        Assert.Equal(62, reference!.BookNumber);
        Assert.Equal(4, reference.Chapter);
        Assert.Equal(8, reference.VerseStart);
    }

    [Fact]
    public void Three_digit_chapters_and_verses_parse()
    {
        var reference = BibleReferenceParser.TryParse("sal 119:105");

        Assert.NotNull(reference);
        Assert.Equal(19, reference!.BookNumber);
        Assert.Equal(119, reference.Chapter);
        Assert.Equal(105, reference.VerseStart);
    }

    [Theory]
    [InlineData("jn. 3:16", 43)]
    [InlineData("1 Co. 13", 46)]
    public void A_dot_after_the_abbreviation_is_ignored(string input, int expectedBook)
    {
        var reference = BibleReferenceParser.TryParse(input);

        Assert.NotNull(reference);
        Assert.Equal(expectedBook, reference!.BookNumber);
    }

    [Fact]
    public void A_space_separated_range_parses()
    {
        var reference = BibleReferenceParser.TryParse("sal 23 1-3");

        Assert.NotNull(reference);
        Assert.Equal(1, reference!.VerseStart);
        Assert.Equal(3, reference.VerseEnd);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("amor")]
    [InlineData("Marciano 3:16")]
    [InlineData("Juan")]
    [InlineData("juan 0")]
    [InlineData("juan 3:0")]
    public void Non_references_return_null(string input)
    {
        Assert.Null(BibleReferenceParser.TryParse(input));
    }

    [Fact]
    public void Descending_verse_range_is_rejected()
    {
        Assert.Null(BibleReferenceParser.TryParse("Juan 3:18-16"));
    }
}
