using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleReferenceFormatterTests
{
    [Fact]
    public void Writes_a_range_a_single_verse_and_a_whole_chapter()
    {
        Assert.Equal("Juan 8:5-10", BibleReferenceFormatter.Format(new BibleReference(43, 8, 5, 10)));
        Assert.Equal("Juan 3:16", BibleReferenceFormatter.Format(new BibleReference(43, 3, 16, 16)));
        Assert.Equal("Juan 3:16", BibleReferenceFormatter.Format(new BibleReference(43, 3, 16, null)));
        Assert.Equal("Salmos 23", BibleReferenceFormatter.Format(new BibleReference(19, 23, null, null)));
    }

    [Fact]
    public void What_it_writes_is_read_back_as_the_same_reference_for_every_book()
    {
        foreach (var book in BibleBookCatalog.Books)
        {
            foreach (var reference in new[]
            {
                new BibleReference(book.Number, 2, null, null),
                new BibleReference(book.Number, 2, 7, 7),
                new BibleReference(book.Number, 12, 3, 9),
            })
            {
                var text = BibleReferenceFormatter.Format(reference);
                Assert.Equal(reference, BibleReferenceParser.TryParse(text));
            }
        }
    }
}
