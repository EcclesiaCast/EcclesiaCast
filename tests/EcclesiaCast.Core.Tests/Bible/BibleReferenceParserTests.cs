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
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("amor")]
    [InlineData("Marciano 3:16")]
    [InlineData("Juan")]
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
