using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleTextCleanerTests
{
    // Texts as the NTV and the TLA files carry them.
    private const string John752 =
        "—¿También tú eres de Galilea? —contestaron ellos—. Estudia las Escrituras y compruébalo tú mismo: jamás ha salido un profeta de Galilea. ---------- [Los manuscritos griegos más antiguos no incluyen Juan 7:53–8:11].";

    private const string John811 =
        "—Ni uno, Señor —dijo ella. —Yo tampoco —le dijo Jesús—. Vete y no peques más. ----------";

    private const string Mark168 =
        "Las mujeres, desconcertadas, huyeron temblando de la tumba y no dijeron nada a nadie porque estaban muy asustadas. ---------- [Los manuscritos más antiguos de Marcos terminan en el versículo 16:8. Otros manuscritos tardíos incluyen uno o ambos de los finales que aparecen a continuación]. [Final breve de Marcos] Luego ellas informaron todo eso a Pedro y a sus compañeros brevemente. Tiempo después, Jesús mismo los envió del oriente al occidente con el sagrado e inagotable mensaje de salvación que da vida eterna. Amén. [Final largo de Marcos]";

    private const string FirstSamuel1027 =
        "Sin embargo, había unos sinvergüenzas que se quejaban: «¿Cómo puede este hombre salvarnos?». Y lo despreciaban y se negaban a llevarle regalos; pero Saúl no les hizo caso. [Nahas, rey de los amonitas, había estado oprimiendo gravemente a los habitantes de Gad y de Rubén que vivían al oriente del río Jordán. Les sacó el ojo derecho a todos los israelitas que vivían allí, y no permitía que nadie viniera a rescatarlos. De hecho, de todos los israelitas que vivían al oriente del río Jordán, no había uno solo a quien Nahas no le hubiera sacado el ojo derecho. Pero había siete mil hombres que habían escapado de los amonitas y se habían establecido en Jabes de Galaad].";

    [Fact]
    public void The_rule_and_its_note_leave_the_end_of_John_7_52()
    {
        Assert.Equal(
            "—¿También tú eres de Galilea? —contestaron ellos—. Estudia las Escrituras y compruébalo tú mismo: jamás ha salido un profeta de Galilea.",
            BibleTextCleaner.Clean(John752));
    }

    [Fact]
    public void A_rule_with_no_note_is_just_removed()
    {
        Assert.Equal(
            "—Ni uno, Señor —dijo ella. —Yo tampoco —le dijo Jesús—. Vete y no peques más.",
            BibleTextCleaner.Clean(John811));
    }

    [Fact]
    public void Mark_16_8_loses_the_note_and_both_labels_but_keeps_the_verses()
    {
        Assert.Equal(
            "Las mujeres, desconcertadas, huyeron temblando de la tumba y no dijeron nada a nadie porque estaban muy asustadas. Luego ellas informaron todo eso a Pedro y a sus compañeros brevemente. Tiempo después, Jesús mismo los envió del oriente al occidente con el sagrado e inagotable mensaje de salvación que da vida eterna. Amén.",
            BibleTextCleaner.Clean(Mark168));
    }

    [Fact]
    public void A_paragraph_in_brackets_that_is_Scripture_stays_as_it_is()
    {
        // 1 Samuel 10:27 en la NTV: el párrafo es texto bíblico.
        Assert.Equal(FirstSamuel1027, BibleTextCleaner.Clean(FirstSamuel1027));
    }

    [Theory]
    [InlineData("Entonces dijo: «Vete en paz». 1 (21.1) Dicho esto, Jonatán regresó.", "Entonces dijo: «Vete en paz». Dicho esto, Jonatán regresó.")]
    [InlineData("Obedeció a Dios en todo. 43b (44) Sin embargo, no destruyó los templos.", "Obedeció a Dios en todo. Sin embargo, no destruyó los templos.")]
    [InlineData("jefe de los treinta soldados más valientes; 4b (5) Jeremías, Jahaziel,", "jefe de los treinta soldados más valientes; Jeremías, Jahaziel,")]
    [InlineData("Dijo esto. 3-4 (4-5) Después de eso se fue.", "Dijo esto. Después de eso se fue.")]
    public void The_Hebrew_numbering_in_the_middle_of_a_verse_goes(string text, string expected)
    {
        Assert.Equal(expected, BibleTextCleaner.Clean(text));
    }

    [Theory]
    [InlineData("Eran unos 7.000 hombres de guerra.")]
    [InlineData("Los doce discípulos se fueron (Sal 23) con él.")]
    [InlineData("Trajo 12 (doce) cabezas de ganado.")]
    [InlineData("Se fue. Luego vio a 72 (hombres) de Israel.")]
    [InlineData("Murieron 70 hombres. 7.000 más huyeron.")]
    [InlineData("Esto está en [Salmos 23] y en (Isaías 53).")]
    public void Ordinary_numbers_and_parentheses_are_left_alone(string text)
    {
        Assert.Equal(text, BibleTextCleaner.Clean(text));
    }

    [Theory]
    [InlineData("Tomó [el] pan y lo partió.")]
    [InlineData("Y dijo [Dios] que sí.")]
    [InlineData("Se acabó. [Dios] habló.")]
    public void Words_the_translator_added_in_brackets_stay(string text)
    {
        Assert.Equal(text, BibleTextCleaner.Clean(text));
    }

    [Fact]
    public void A_verse_with_nothing_to_clean_comes_back_untouched_double_spaces_included()
    {
        const string text = "Dos  espacios y nada más.";
        Assert.Same(text, BibleTextCleaner.Clean(text));
    }

    [Fact]
    public void Four_underscores_count_as_a_rule_but_three_dashes_do_not()
    {
        Assert.Equal("Fin.", BibleTextCleaner.Clean("Fin. ____"));
        Assert.Equal("Y así --- fue.", BibleTextCleaner.Clean("Y así --- fue."));
    }

    [Theory]
    [InlineData("Dios es nuestro amparo. Selah", "Dios es nuestro amparo.")]
    [InlineData("Dios es nuestro amparo. Selah.", "Dios es nuestro amparo.")]
    [InlineData("Dios es nuestro amparo Selah.", "Dios es nuestro amparo.")]
    [InlineData("Jehová de los ejércitos está con nosotros, Selah", "Jehová de los ejércitos está con nosotros,")]
    [InlineData("Alabadle; selah; y cantad.", "Alabadle; y cantad.")]
    [InlineData("Dios (Selah) es nuestro amparo.", "Dios es nuestro amparo.")]
    public void Selah_goes_when_the_theme_hides_it(string text, string expected)
    {
        Assert.Equal(expected, BibleTextCleaner.Clean(text, showSelah: false));
    }

    [Fact]
    public void Selah_stays_when_the_theme_shows_it()
    {
        Assert.Equal("Dios es nuestro amparo. Selah", BibleTextCleaner.Clean("Dios es nuestro amparo. Selah"));
    }

    [Fact]
    public void Words_that_merely_contain_Selah_are_not_touched()
    {
        const string text = "Los de Selahim y Selahot se juntaron.";
        Assert.Equal(text, BibleTextCleaner.Clean(text, showSelah: false));
    }

    [Fact]
    public void The_Psalm_heading_is_verse_zero_of_Psalms_only()
    {
        Assert.True(BibleTextCleaner.IsPsalmTitle(19, 0));
        Assert.False(BibleTextCleaner.IsPsalmTitle(19, 1));
        Assert.False(BibleTextCleaner.IsPsalmTitle(1, 0));
    }

    private static BibleVerseResult V(int book, int verse, string text) => new(book, "Libro", 3, verse, text);

    [Fact]
    public void The_Psalm_heading_is_dropped_from_a_list_unless_the_theme_shows_it()
    {
        IReadOnlyList<BibleVerseResult> verses = [V(19, 0, "Al músico principal. Salmo de David."), V(19, 1, "Bienaventurado el varón."), V(1, 0, "Prólogo")];

        Assert.Equal([1, 0], BibleTextCleaner.WithoutHiddenTitles(verses, showSelahAndTitles: false).Select(v => v.Verse));
        Assert.Equal(3, BibleTextCleaner.WithoutHiddenTitles(verses, showSelahAndTitles: true).Count);
    }

    [Fact]
    public void A_list_shows_the_text_cleaned_with_the_leading_numbers_gone_too()
    {
        IReadOnlyList<BibleVerseResult> verses = [V(11, 43, "43 (44) Obedeció en todo. 43b (45) Pero no destruyó. Selah")];

        var shown = BibleTextCleaner.ForList(verses, showSelahAndTitles: false);

        Assert.Equal("Obedeció en todo. Pero no destruyó.", shown[0].Text);
        Assert.Equal("43 (44) Obedeció en todo. 43b (45) Pero no destruyó. Selah", verses[0].Text);
    }

    [Fact]
    public void Empty_text_is_fine()
    {
        Assert.Equal(string.Empty, BibleTextCleaner.Clean(null));
        Assert.Equal(string.Empty, BibleTextCleaner.Clean(""));
    }
}
