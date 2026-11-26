using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class VerseSplitterTests
{
    private const string John316 =
        "Porque de tal manera amó Dios al mundo, que ha dado a su Hijo unigénito, para que todo aquel " +
        "que en él cree, no se pierda, mas tenga vida eterna.";

    private static string[] Mains(string text, int max) =>
        VerseSplitter.Split(text, null, max).Select(p => p.Main).ToArray();

    private static void AssertSameWords(string text, IEnumerable<string> parts) =>
        Assert.Equal(
            text.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            string.Join(" ", parts).Split(' ', StringSplitOptions.RemoveEmptyEntries));

    [Fact]
    public void Off_leaves_the_verse_whole()
    {
        var part = Assert.Single(VerseSplitter.Split(John316, "Otra versión del mismo versículo.", 0));

        Assert.Equal(John316, part.Main);
        Assert.Equal("Otra versión del mismo versículo.", part.Secondary);
    }

    [Fact]
    public void A_short_text_is_not_touched()
    {
        var part = Assert.Single(VerseSplitter.Split("  Jesús lloró. ", null, 150));

        Assert.Equal("  Jesús lloró. ", part.Main);
        Assert.Null(part.Secondary);
    }

    [Fact]
    public void A_text_of_exactly_the_maximum_is_one_part()
    {
        Assert.Single(Mains(new string('a', 24) + " " + new string('b', 25), 50));
    }

    [Fact]
    public void A_period_near_the_middle_wins_over_a_closer_comma()
    {
        const string first = "El Señor es mi pastor y nada me va a faltar nunca.";
        const string second = "En verdes praderas, junto a aguas tranquilas me hace descansar siempre";

        Assert.Equal([first, second], Mains($"{first} {second}", 80));
    }

    [Fact]
    public void A_semicolon_counts_like_a_period()
    {
        const string first = "Confía en el Señor con todo tu corazón siempre;";
        const string second = "no te apoyes, hijo mío, en tu propia prudencia jamás";

        Assert.Equal([first, second], Mains($"{first} {second}", 70));
    }

    [Fact]
    public void A_period_before_a_closing_quote_is_a_period()
    {
        const string first = "Entonces él les dijo: «Vengan conmigo ahora mismo.»";
        const string second = "Y ellos dejaron sus redes, y lo siguieron sin dudar nada";

        Assert.Equal([first, second], Mains($"{first} {second}", 70));
    }

    [Fact]
    public void Without_a_near_period_it_cuts_at_a_comma()
    {
        var parts = Mains(John316, 100);

        Assert.Equal(2, parts.Length);
        Assert.EndsWith("unigénito,", parts[0]);
        AssertSameWords(John316, parts);
    }

    [Fact]
    public void A_period_far_from_the_cut_is_passed_over_and_leaves_no_loose_words()
    {
        const string text =
            "Jesús dijo. Yo soy el camino y la verdad y la vida y nadie viene al Padre, " +
            "sino por mí y por el amor que les tengo desde el principio de todo";

        var parts = Mains(text, 100);

        Assert.Equal(2, parts.Length);
        Assert.EndsWith("al Padre,", parts[0]);
    }

    [Fact]
    public void Without_punctuation_it_cuts_between_words_in_similar_parts()
    {
        var text = string.Join(" ", Enumerable.Repeat("palabra", 40)); // 319 letras

        var parts = Mains(text, 150);

        Assert.Equal(3, parts.Length);
        Assert.All(parts, p => Assert.InRange(p.Length, 95, 120));
        AssertSameWords(text, parts);
    }

    [Fact]
    public void One_letter_over_the_maximum_gives_two_halves_not_a_loose_word()
    {
        var text = string.Join(" ", Enumerable.Repeat("amor", 31)); // 154 letras

        var parts = Mains(text, 150);

        Assert.Equal(2, parts.Length);
        Assert.All(parts, p => Assert.InRange(p.Length, 70, 85));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(80)]
    [InlineData(150)]
    public void No_part_goes_over_the_maximum_and_no_word_is_lost(int max)
    {
        var parts = Mains(John316, max);

        Assert.All(parts, p => Assert.True(p.Length <= max, $"{p.Length} > {max}: {p}"));
        AssertSameWords(John316, parts);
    }

    [Fact]
    public void A_word_longer_than_the_maximum_stays_whole()
    {
        var word = new string('x', 70);

        var parts = Mains($"antes {word} después", 50);

        Assert.Equal(["antes", word, "después"], parts);
    }

    [Fact]
    public void A_single_word_longer_than_the_maximum_is_one_part()
    {
        var word = new string('x', 200);

        Assert.Equal([word], Mains(word, 50));
    }

    [Fact]
    public void A_dash_standing_alone_does_not_start_a_part()
    {
        var text = string.Join(" — ", Enumerable.Repeat("una frase de varias palabras", 4));

        Assert.All(Mains(text, 50), p => Assert.False(p.StartsWith('—')));
    }

    [Fact]
    public void Two_versions_take_as_many_parts_as_the_longer_one_needs()
    {
        const string shorter = "Tanto amó Dios al mundo, que dio a su Hijo, para que todo el que crea tenga vida.";

        var parts = VerseSplitter.Split(shorter, John316, 100);

        Assert.Equal(2, parts.Count);
        AssertSameWords(shorter, parts.Select(p => p.Main));
        AssertSameWords(John316, parts.Select(p => p.Secondary!));
        Assert.EndsWith("que dio a su Hijo,", parts[0].Main);
        Assert.EndsWith("unigénito,", parts[0].Secondary);
    }

    [Fact]
    public void A_short_second_version_is_cut_at_proportional_points()
    {
        const string second = "uno dos tres cuatro cinco seis siete ocho nueve diez once doce";
        var main = string.Join(" ", Enumerable.Repeat("palabra", 40));

        var parts = VerseSplitter.Split(main, second, 150);

        Assert.Equal(3, parts.Count);
        Assert.All(parts, p => Assert.InRange(p.Secondary!.Length, 12, 28));
        AssertSameWords(second, parts.Select(p => p.Secondary!));
    }

    [Fact]
    public void A_version_with_fewer_words_than_parts_leaves_the_last_ones_empty()
    {
        var main = string.Join(" ", Enumerable.Repeat("palabra", 40));

        var parts = VerseSplitter.Split(main, "Omitido.", 150);

        Assert.Equal(["Omitido.", null, null], parts.Select(p => p.Secondary));
        AssertSameWords(main, parts.Select(p => p.Main));
    }

    [Fact]
    public void A_whole_paragraph_of_grouped_verses_is_cut_too()
    {
        var paragraph = string.Join(" ", Enumerable.Repeat(John316, 8));

        var parts = Mains(paragraph, 150);

        Assert.All(parts, p => Assert.InRange(p.Length, 40, 150));
        AssertSameWords(paragraph, parts);
    }
}
