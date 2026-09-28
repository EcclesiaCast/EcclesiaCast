using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleBookCatalogTests
{
    [Fact]
    public void Has_exactly_66_books()
    {
        Assert.Equal(66, BibleBookCatalog.Books.Count);
        Assert.Equal(Enumerable.Range(1, 66), BibleBookCatalog.Books.Select(b => b.Number).OrderBy(n => n));
    }

    [Theory]
    [InlineData("Juan", 43)]
    [InlineData("juan", 43)]
    [InlineData("jn", 43)]
    [InlineData("JN", 43)]
    [InlineData("1 Corintios", 46)]
    [InlineData("1co", 46)]
    [InlineData("1 co", 46)]
    [InlineData("Génesis", 1)]
    [InlineData("genesis", 1)]
    [InlineData("Gn", 1)]
    [InlineData("Sal", 19)]
    [InlineData("Salmos", 19)]
    [InlineData("Ap", 66)]
    [InlineData("Apocalipsis", 66)]
    public void Finds_books_by_full_name_or_abbreviation(string text, int expectedNumber)
    {
        var book = BibleBookCatalog.FindByName(text);

        Assert.NotNull(book);
        Assert.Equal(expectedNumber, book!.Number);
    }

    [Fact]
    public void Unknown_book_returns_null()
    {
        Assert.Null(BibleBookCatalog.FindByName("Marciano"));
    }

    [Fact]
    public void Numbered_books_do_not_collide_with_each_other()
    {
        Assert.Equal(9, BibleBookCatalog.FindByName("1s")!.Number);
        Assert.Equal(10, BibleBookCatalog.FindByName("2s")!.Number);
        Assert.Equal(62, BibleBookCatalog.FindByName("1jn")!.Number);
        Assert.Equal(63, BibleBookCatalog.FindByName("2jn")!.Number);
        Assert.Equal(64, BibleBookCatalog.FindByName("3jn")!.Number);
    }

    private static int[] Starting(string typed) =>
        BibleBookCatalog.Books.Where(b => BibleBookCatalog.StartsWith(b, typed)).Select(b => b.Number).ToArray();

    [Fact]
    public void Typing_the_start_of_a_name_narrows_to_the_books_it_fits()
    {
        Assert.Equal([7, 43, 65], Starting("ju"));
        Assert.Equal([43], Starting("juan"));
        Assert.Equal([19], Starting("salm"));
    }

    [Fact]
    public void The_start_ignores_accents_case_and_spaces()
    {
        Assert.Equal([1], Starting("GÉN"));
        Assert.Equal([62], Starting("1 jua"));
    }

    [Fact]
    public void Abbreviations_count_as_a_start_too()
    {
        // "jn" is how Juan is abbreviated, even though no name starts with it.
        Assert.Contains(43, Starting("jn"));
    }

    [Fact]
    public void Nothing_typed_fits_every_book()
    {
        Assert.Equal(66, Starting("  ").Length);
    }

    [Fact]
    public void Text_that_starts_no_book_fits_none()
    {
        Assert.Empty(Starting("paz"));
    }
}
