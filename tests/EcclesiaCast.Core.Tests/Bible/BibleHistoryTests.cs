using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleHistoryTests
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 11, 12, 0, 0, TimeSpan.FromHours(-3));

    private static BibleHistoryEntry Entry(
        int book = 43, int chapter = 3, int verse = 16, int? end = null,
        int? version = 1, string abbreviation = "NTV", int? secondary = null) =>
        new(book, chapter, verse, end, version, abbreviation, secondary, Noon);

    [Fact]
    public void El_texto_para_mostrar_lleva_la_cita_y_la_version()
    {
        Assert.Equal("Juan 3:16 · NTV", Entry().Display);
    }

    [Fact]
    public void El_texto_para_mostrar_de_un_tramo_lleva_los_dos_versiculos()
    {
        Assert.Equal("Juan 3:16-18 · NTV", Entry(end: 18).Display);
    }

    [Fact]
    public void Sin_abreviatura_el_texto_para_mostrar_es_solo_la_cita()
    {
        Assert.Equal("Juan 3:16", Entry(abbreviation: "").Display);
    }

    [Fact]
    public void Lo_nuevo_queda_arriba()
    {
        var list = BibleHistoryList.Add([Entry(verse: 16)], Entry(verse: 17));

        Assert.Equal([17, 16], list.Select(e => e.VerseStart));
    }

    [Fact]
    public void El_mismo_pasaje_no_se_repite_y_sube_al_primer_lugar()
    {
        var older = new[] { Entry(verse: 1), Entry(verse: 16), Entry(verse: 20) };

        var list = BibleHistoryList.Add(older, Entry(verse: 16));

        Assert.Equal([16, 1, 20], list.Select(e => e.VerseStart));
    }

    [Fact]
    public void El_mismo_pasaje_en_otra_version_reemplaza_al_anterior()
    {
        var list = BibleHistoryList.Add([Entry(version: 1, abbreviation: "NTV")], Entry(version: 2, abbreviation: "RV60"));

        var only = Assert.Single(list);
        Assert.Equal("RV60", only.VersionAbbreviation);
    }

    [Fact]
    public void Un_tramo_distinto_del_mismo_versiculo_es_otra_entrada()
    {
        var list = BibleHistoryList.Add([Entry(verse: 16)], Entry(verse: 16, end: 18));

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Un_final_igual_al_comienzo_es_el_mismo_pasaje_que_sin_final()
    {
        var list = BibleHistoryList.Add([Entry(verse: 16, end: null)], Entry(verse: 16, end: 16));

        Assert.Single(list);
    }

    [Fact]
    public void La_lista_se_recorta_al_tope_y_se_van_las_mas_viejas()
    {
        IReadOnlyList<BibleHistoryEntry> list = [];
        for (var verse = 1; verse <= 35; verse++)
            list = BibleHistoryList.Add(list, Entry(verse: verse));

        Assert.Equal(30, list.Count);
        Assert.Equal(35, list[0].VerseStart);
        Assert.Equal(6, list[^1].VerseStart);
    }

    [Fact]
    public void El_tope_se_puede_elegir()
    {
        var list = BibleHistoryList.Add([Entry(verse: 1), Entry(verse: 2)], Entry(verse: 3), capacity: 2);

        Assert.Equal([3, 1], list.Select(e => e.VerseStart));
    }

    [Fact]
    public void Lo_que_se_guarda_se_lee_igual()
    {
        var list = new[]
        {
            Entry(verse: 16, end: 18, secondary: 7),
            Entry(book: 46, chapter: 13, verse: 4, version: null, abbreviation: ""),
        };

        var back = BibleHistoryList.Parse(BibleHistoryList.Serialize(list));

        Assert.Equal(list, back);
    }

    [Fact]
    public void Lo_guardado_no_repite_el_texto_que_se_arma_solo()
    {
        var text = BibleHistoryList.Serialize([Entry()]);

        Assert.DoesNotContain("Display", text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("basura")]
    [InlineData("{\"a\":1}")]
    [InlineData("[1,2,3]")]
    public void Un_texto_vacio_o_roto_da_una_lista_vacia(string? text)
    {
        Assert.Empty(BibleHistoryList.Parse(text));
    }

    [Fact]
    public void Las_entradas_que_no_tienen_sentido_se_descartan()
    {
        var text = BibleHistoryList.Serialize(
        [
            Entry(book: 0),
            Entry(book: 67),
            Entry(chapter: 0),
            Entry(verse: 0),
            Entry(verse: 5, end: 2),
            Entry(verse: 9),
        ]);

        var back = BibleHistoryList.Parse(text);

        var kept = Assert.Single(back);
        Assert.Equal(9, kept.VerseStart);
    }

    [Fact]
    public void Una_entrada_nula_en_la_lista_se_descarta_y_las_demas_quedan()
    {
        var one = BibleHistoryList.Serialize([Entry(verse: 3)]).TrimStart('[');
        var text = "[null," + one;

        Assert.Single(BibleHistoryList.Parse(text));
    }

    [Fact]
    public void Al_leer_tambien_se_respeta_el_tope()
    {
        var many = Enumerable.Range(1, 40).Select(v => Entry(verse: v)).ToList();

        Assert.Equal(30, BibleHistoryList.Parse(BibleHistoryList.Serialize(many)).Count);
    }

    [Fact]
    public void La_cita_de_la_entrada_vuelve_igual_por_el_lector_de_citas_con_un_libro_numerado()
    {
        var entry = Entry(book: 46, chapter: 13, verse: 4, end: 7);

        var parsed = BibleReferenceParser.TryParse(entry.Reference);

        Assert.Equal(entry.ToReference(), parsed);
        Assert.Equal("1 Corintios 13:4-7", entry.Reference);
    }
}
