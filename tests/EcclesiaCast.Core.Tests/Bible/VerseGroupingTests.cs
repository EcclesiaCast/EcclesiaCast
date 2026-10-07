using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class VerseGroupingTests
{
    private static BibleVerseResult V(int verse, string text, int chapter = 32) =>
        new(1, "Génesis", chapter, verse, text);

    [Fact]
    public void Each_verse_is_its_own_slide_normally()
    {
        var groups = VerseGrouping.Group([V(1, "Uno."), V(2, "Dos."), V(3, "Tres.")]);

        Assert.Equal(["1", "2", "3"], groups.Select(g => g.Numbers));
        Assert.Equal("Dos.", groups[1].Text);
    }

    [Fact]
    public void A_paragraph_repeated_under_several_verses_becomes_one_slide()
    {
        // Así viene la TLA: el mismo párrafo debajo de cada versículo que cubre.
        const string paragraph = "3-4 (4-5) Después de eso, Jacob envió unos mensajeros.";
        var groups = VerseGrouping.Group([
            V(2, "2 (3) Cuando Jacob los vio, dijo."),
            V(3, paragraph),
            V(4, paragraph),
            V(5, "5 (6) Ahora es dueño de vacas."),
        ]);

        Assert.Equal(["2", "3-4", "5"], groups.Select(g => g.Numbers));
        Assert.Equal("Después de eso, Jacob envió unos mensajeros.", groups[1].Text);
        Assert.True(groups[1].Contains(4));
        Assert.False(groups[1].Contains(5));
    }

    [Fact]
    public void The_same_text_in_another_chapter_or_after_a_gap_is_not_merged()
    {
        var groups = VerseGrouping.Group([V(1, "Amén."), V(3, "Amén."), V(4, "Amén.", chapter: 33)]);

        Assert.Equal(3, groups.Count);
    }

    [Fact]
    public void A_paragraph_in_the_second_version_groups_the_first_one_too()
    {
        var secondary = new Dictionary<(int, int), string>
        {
            [(32, 3)] = "3-4 (4-5) Párrafo de la TLA.",
            [(32, 4)] = "3-4 (4-5) Párrafo de la TLA.",
        };

        var groups = VerseGrouping.Group([V(3, "Versículo tres."), V(4, "Versículo cuatro.")], secondary);

        var group = Assert.Single(groups);
        Assert.Equal("Versículo tres. Versículo cuatro.", group.Text);
        Assert.Equal("Párrafo de la TLA.", group.SecondaryText);
    }

    [Theory]
    [InlineData("2 (3) Cuando Jacob", 2, "Cuando Jacob")]
    [InlineData("3-4 (4-5) Después", 4, "Después")]
    [InlineData("2-18 (1-17) Por ello", 5, "Por ello")]
    [InlineData("55 (32.1) A la mañana", 55, "A la mañana")]
    [InlineData("1 (1b) Dios es mi protector", 1, "Dios es mi protector")]
    [InlineData("11-12 Tú me prometiste", 11, "Tú me prometiste")]
    public void Leading_verse_numbers_are_dropped(string text, int verse, string expected) =>
        Assert.Equal(expected, VerseGrouping.StripVerseNumbers(text, verse));

    [Theory]
    [InlineData("72.000 cabezas de ganado,", 33)]
    [InlineData("40 días y 40 noches", 40)]
    [InlineData("3-4 (4-5) De otro versículo", 7)]
    public void A_number_that_belongs_to_the_text_stays(string text, int verse) =>
        Assert.Equal(text, VerseGrouping.StripVerseNumbers(text, verse));
}
