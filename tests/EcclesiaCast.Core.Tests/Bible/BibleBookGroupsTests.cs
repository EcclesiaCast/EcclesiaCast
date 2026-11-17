using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleBookGroupsTests
{
    [Theory]
    [InlineData(1, BibleBookGroup.Law)]
    [InlineData(5, BibleBookGroup.Law)]
    [InlineData(6, BibleBookGroup.History)]
    [InlineData(17, BibleBookGroup.History)]
    [InlineData(19, BibleBookGroup.Poetry)]
    [InlineData(23, BibleBookGroup.MajorProphets)]
    [InlineData(27, BibleBookGroup.MajorProphets)]
    [InlineData(28, BibleBookGroup.MinorProphets)]
    [InlineData(39, BibleBookGroup.MinorProphets)]
    [InlineData(40, BibleBookGroup.Gospels)]
    [InlineData(43, BibleBookGroup.Gospels)]
    [InlineData(44, BibleBookGroup.Acts)]
    [InlineData(45, BibleBookGroup.PaulLetters)]
    [InlineData(57, BibleBookGroup.PaulLetters)]
    [InlineData(58, BibleBookGroup.GeneralLetters)]
    [InlineData(65, BibleBookGroup.GeneralLetters)]
    [InlineData(66, BibleBookGroup.Revelation)]
    public void Each_book_falls_in_its_group(int book, BibleBookGroup expected) =>
        Assert.Equal(expected, BibleBookGroups.GroupOf(book));

    [Fact]
    public void The_66_books_use_all_ten_groups_in_order()
    {
        var groups = BibleBookGroups.Arrange(BibleBookCatalog.Books);
        Assert.Equal(10, groups.Count);
        Assert.Equal(66, groups.Sum(g => g.Books.Count));
        Assert.Equal(groups.Select(g => g.Group).OrderBy(g => g), groups.Select(g => g.Group));
    }

    [Fact]
    public void Every_group_has_a_name_and_its_own_colour()
    {
        var all = Enum.GetValues<BibleBookGroup>();
        Assert.All(all, g => Assert.False(string.IsNullOrWhiteSpace(BibleBookGroups.GroupName(g))));
        Assert.Equal(all.Length, all.Select(BibleBookGroups.ColorHex).Distinct().Count());
        Assert.All(all, g => Assert.Matches("^#[0-9A-F]{6}$", BibleBookGroups.ColorHex(g)));
    }

    [Theory]
    [InlineData(1, "Gn")]
    [InlineData(2, "Ex")]
    [InlineData(43, "Jn")]
    [InlineData(46, "1Co")]
    [InlineData(9, "1S")]
    [InlineData(19, "Sal")]
    public void Abbreviation_is_the_short_name_with_capitals(int book, string expected) =>
        Assert.Equal(expected, BibleBookGroups.Abbreviation(BibleBookCatalog.FindByNumber(book)!));

    [Fact]
    public void Only_the_books_a_version_has_are_arranged()
    {
        var onlyNewTestament = BibleBookCatalog.Books.Where(b => b.Number >= 40).ToList();
        var groups = BibleBookGroups.Arrange(onlyNewTestament);
        Assert.Equal(5, groups.Count);
        Assert.Equal(BibleBookGroup.Gospels, groups[0].Group);
        Assert.Equal(27, groups.Sum(g => g.Books.Count));
    }
}
