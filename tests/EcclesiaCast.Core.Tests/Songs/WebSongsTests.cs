using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Songs.Web;

namespace EcclesiaCast.Core.Tests.Songs;

public class WebLyricsFormatterTests
{
    [Fact]
    public void Paragraphs_become_slides()
    {
        var text = WebLyricsFormatter.FromParagraphs(["Uno\ndos", "Tres\ncuatro"]);

        Assert.Equal("Uno\ndos\n\nTres\ncuatro", text);
    }

    [Fact]
    public void Section_names_written_in_the_lyrics_become_tags()
    {
        var text = WebLyricsFormatter.FromParagraphs(
            ["Verso 1:\nSeñor mi Dios\nal contemplar los cielos", "CORO\nMi corazón\nentona la canción", "(Puente)\nCuán grande es Él"]);

        var sections = LyricsParser.Parse(text);
        Assert.Equal(["Verso 1", "Coro", "Puente"], sections.Select(s => s.Label));
        Assert.Equal("Señor mi Dios\nal contemplar los cielos", sections[0].Text);
        Assert.DoesNotContain(sections, s => s.Text.Contains("CORO", StringComparison.Ordinal));
    }

    [Fact]
    public void A_name_followed_by_text_on_the_same_line_is_split()
    {
        var sections = LyricsParser.Parse(WebLyricsFormatter.FromParagraphs(["Coro: Mi corazón\nentona la canción"]));

        Assert.Equal("Coro", sections[0].Label);
        Assert.Equal("Mi corazón\nentona la canción", sections[0].Text);
    }

    [Fact]
    public void A_lyric_line_that_merely_starts_like_a_name_is_kept()
    {
        var sections = LyricsParser.Parse(WebLyricsFormatter.FromParagraphs(["Coros de ángeles cantan\nal Rey"]));

        Assert.Equal("Coros de ángeles cantan\nal Rey", sections[0].Text);
    }

    [Fact]
    public void Long_paragraphs_are_split_into_balanced_slides()
    {
        var lines = Enumerable.Range(1, 10).Select(i => $"línea {i}").ToList();

        var chunks = WebLyricsFormatter.Split(lines).ToList();

        Assert.Equal([4, 3, 3], chunks.Select(c => c.Count));
        Assert.Equal(lines, chunks.SelectMany(c => c));
    }

    [Fact]
    public void Paragraphs_that_fit_are_left_alone()
    {
        var lines = Enumerable.Range(1, WebLyricsFormatter.MaxLinesPerSlide).Select(i => $"{i}").ToList();

        Assert.Single(WebLyricsFormatter.Split(lines));
    }

    [Fact]
    public void Plain_text_is_split_on_blank_lines()
    {
        var text = WebLyricsFormatter.FromPlainText("Uno\r\ndos\r\n\r\n\r\nTres  \n  \ncuatro");

        Assert.Equal("Uno\ndos\n\nTres\n\ncuatro", text);
    }
}

public class MusicaComParserTests
{
    private const string QuickSearch = """
        <table style="border-spacing:5px">
        <tr style="cursor:pointer" onclick="window.location='https://www.musica.com/letras.asp?letras=17196'"><td><a href="https://www.musica.com/letras.asp?letras=17196"><img src=x.png></a></td><td><a href="https://www.musica.com/letras.asp?letras=17196"><b>Letras de <font style=color:#18739a><b>Marcos Witt</b></font></b></a></td></tr>
        <tr style="cursor:pointer" onclick="window.location='https://www.musica.com/letras.asp?letra=904869'"><td><a href="https://www.musica.com/letras.asp?letra=904869"><img src=x.jpg width="48"></a></td><td><a href="https://www.musica.com/letras.asp?letra=904869"><b>Marcos Witt</b><br><font style=color:#18739a><b>Renuévame</b></font></a></td></tr>
        <tr style="cursor:pointer" onclick="window.location='https://www.musica.com/letras.asp?letra=2642186'"><td><a href="https://www.musica.com/letras.asp?letra=2642186"><img src=x.jpg></a></td><td><a href="https://www.musica.com/letras.asp?letra=2642186"><b>Marcos Witt</b><br>Cuán Grande Es Él (ft. Marcos Vidal, Marco Barrientos)</a></td></tr>
        <tr style="cursor:pointer" onclick="window.location='letras.asp?q=renuevame&ref=si'"><td colspan="2"><a href="letras.asp?q=renuevame&ref=si">Ver más resultados</a></td></tr>
        </table>
        """;

    [Fact]
    public void Quick_search_lists_songs_with_artist_and_title()
    {
        var hits = MusicaComParser.ParseQuickSearch(QuickSearch);

        Assert.Equal(2, hits.Count);
        Assert.Equal("Renuévame", hits[0].Title);
        Assert.Equal("Marcos Witt", hits[0].Artist);
        Assert.Equal("https://www.musica.com/letras.asp?letra=904869", hits[0].Url);
        Assert.Equal(WebSongSource.MusicaCom, hits[0].Source);
    }

    [Fact]
    public void Featured_artists_are_dropped_from_the_title()
    {
        var hits = MusicaComParser.ParseQuickSearch(QuickSearch);

        Assert.Equal("Cuán Grande Es Él", hits[1].Title);
    }

    [Fact]
    public void Full_search_results_are_read_from_the_page_title()
    {
        var hit = MusicaComParser.ParseFullSearchResult(
            "https://www.musica.com/letras.asp?letra=2511806",
            "Cuán grande es él - Letra - Himnos del Evangelio - Musica.com");

        Assert.NotNull(hit);
        Assert.Equal("Cuán grande es él", hit.Title);
        Assert.Equal("Himnos del Evangelio", hit.Artist);
    }

    [Fact]
    public void Full_search_ignores_links_that_are_not_lyrics()
    {
        Assert.Null(MusicaComParser.ParseFullSearchResult(
            "https://www.musica.com/letras.asp?letras=17196", "Letras de Marcos Witt - Musica.com"));
    }

    private const string LyricsPage = """
        <html><head><title>Cuán Grande Es Él - Letra - Marcos Witt - Musica.com</title>
        <script type="application/ld+json">{"@context":"http://schema.org/","@type":"MusicRecording","name":"Cuán Grande Es Él (ft. Marcos Vidal, Marco Barrientos)","byArtist":{"@type":"MusicGroup","name":"Marcos Witt"}}</script>
        </head><body><main><article><div class="letra"><div class="info"><h1>Cuán Grande Es Él</h1></div>
        <div id="letra" style="text-align:center;font-size:18px">
        <h2>LETRA</h2>
        <p>
        <strong>Cuán Grande Es Él</strong>
        </p>
        <p>Verso 1:<br>Señor mi Dios<br>Al contemplar los cielos</p>
        <div data-fuse="23266531974" class="vincontent">
        </div>
        <p>Coro:<br>Mi corazón<br>Entona la canción<br>Cuán grande es &Eacute;l</p>
        </div>
        </div>
        <div><div id="significado"><p>Esta canción nos invita a maravillarnos.</p></div></div>
        </article></main></body></html>
        """;

    [Fact]
    public void Lyrics_page_gives_title_artist_and_slides()
    {
        var song = MusicaComParser.ParseLyricsPage(LyricsPage);

        Assert.NotNull(song);
        Assert.Equal("Cuán Grande Es Él", song.Title);
        Assert.Equal("Marcos Witt", song.Artist);

        var sections = LyricsParser.Parse(song.Lyrics);
        Assert.Equal(2, sections.Count);
        Assert.Equal("Verso 1", sections[0].Label);
        Assert.Equal("Señor mi Dios\nAl contemplar los cielos", sections[0].Text);
        Assert.Equal("Coro", sections[1].Label);
        Assert.Equal("Mi corazón\nEntona la canción\nCuán grande es Él", sections[1].Text);
    }

    [Fact]
    public void Text_after_the_lyrics_block_is_not_imported()
    {
        var song = MusicaComParser.ParseLyricsPage(LyricsPage);

        Assert.DoesNotContain("maravillarnos", song!.Lyrics);
        Assert.DoesNotContain("Cuán Grande Es Él\n", song.Lyrics);
    }

    [Fact]
    public void A_page_without_lyrics_gives_nothing()
    {
        Assert.Null(MusicaComParser.ParseLyricsPage("<html><body><p>Hola</p></body></html>"));
    }

    [Fact]
    public void Page_title_is_the_fallback_for_title_and_artist()
    {
        var html = LyricsPage.Replace("application/ld+json", "text/plain");

        var song = MusicaComParser.ParseLyricsPage(html);

        Assert.Equal("Cuán Grande Es Él", song!.Title);
        Assert.Equal("Marcos Witt", song.Artist);
    }
}

public class LrclibParserTests
{
    private const string Search = """
        [
          {"id":1,"trackName":"Renuévame","artistName":"Marcos Witt","albumName":"A","instrumental":false,"plainLyrics":"Renuévame, Señor Jesús\nYa no quiero ser igual\n\nPorque todo lo que hay\ndentro de mí"},
          {"id":2,"trackName":"Renuevame","artistName":"Marcos Witt","albumName":"B","instrumental":false,"plainLyrics":"Renuévame, Señor Jesús"},
          {"id":3,"trackName":"Renuévame","artistName":"Marcos Witt","albumName":"C","instrumental":false,"plainLyrics":null},
          {"id":4,"trackName":"Intro","artistName":"Otro","albumName":"D","instrumental":true,"plainLyrics":""},
          {"id":5,"trackName":"Renuévame","artistName":"Maverick City Música","albumName":"E","instrumental":false,"plainLyrics":"Renuévame"}
        ]
        """;

    [Fact]
    public void Keeps_one_result_per_song_with_lyrics()
    {
        var hits = LrclibParser.ParseSearch(Search);

        Assert.Equal(2, hits.Count);
        Assert.Equal("Marcos Witt", hits[0].Artist);
        Assert.Equal("Maverick City Música", hits[1].Artist);
        Assert.All(hits, h => Assert.Equal(WebSongSource.Lrclib, h.Source));
    }

    [Fact]
    public void Results_carry_the_lyrics_split_into_slides()
    {
        var hit = LrclibParser.ParseSearch(Search)[0];

        Assert.Equal(2, LyricsParser.Parse(hit.Lyrics).Count);
    }

    [Fact]
    public void Accents_and_case_do_not_make_a_different_song()
    {
        Assert.Equal(LrclibParser.Key("Renuévame, Señor"), LrclibParser.Key("RENUEVAME senor"));
    }
}
