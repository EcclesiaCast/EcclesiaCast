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
}
