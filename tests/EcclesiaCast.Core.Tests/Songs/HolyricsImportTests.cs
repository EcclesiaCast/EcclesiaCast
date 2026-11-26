using System.Text;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

/// <summary>
/// Holyrics is read from the files its free plan saves (Canción → Exportar, or
/// Archivo → Guardar). The samples have the shape of what Holyrics 2.30 saved
/// on 25/11/2026.
/// </summary>
public class HolyricsImportTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ecclesia-holyrics-" + Guid.NewGuid().ToString("N"));

    public HolyricsImportTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Write(string name, string content, bool bom = false)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllText(path, content, new UTF8Encoding(bom));
        return path;
    }

    private const string BatchJson = """
        [
          { "id": 1611686353330, "title": " Holyrics", "artist": "Software & App",
            "lyrics": { "paragraphs": [ { "number": 1, "description": "", "text": "Welcome to Holyrics!" } ] } },
          { "id": 1795171779257, "title": "Más fuerte que nunca", "artist": "", "author": "Marcos Witt",
            "copyright": "© 1999",
            "lyrics": { "paragraphs": [
              { "number": 1, "description": "", "text": "Por ti los cojos danzan\nPor ti los mudos cantan" },
              { "number": 2, "description": "Chorus", "text": "Tu nombre exaltamos\r\nEres señor de la creación" },
              { "number": 3, "description": "Verse 2", "text": "" },
              { "number": 4, "description": "Ponte", "text": "Los redimidos cantan" } ] } },
          { "id": 1795620801678, "title": "Las Calles", "artist": "Rescate", "archived": false,
            "lyrics": { "paragraphs": [ { "number": 1, "description": "", "text": "Fotos de la vida." } ] } },
          { "id": 1, "title": "Guardada", "archived": true,
            "lyrics": { "paragraphs": [ { "number": 1, "description": "", "text": "No se trae" } ] } }
        ]
        """;

    [Fact]
    public void A_batch_json_gives_every_song_but_the_welcome_one_and_the_archived()
    {
        var songs = HolyricsImporter.FromFile(Write("lote.json", BatchJson));

        Assert.Equal(["Más fuerte que nunca", "Las Calles"], songs.Select(s => s.Title));
        Assert.Equal("Rescate", songs[1].Artist);
    }

    [Fact]
    public void Each_paragraph_is_a_section_labelled_in_spanish()
    {
        var song = HolyricsImporter.FromFile(Write("lote.json", BatchJson))[0];

        Assert.Equal(["1", "Coro", "Puente"], song.Sections.Select(s => s.Label));
        Assert.Equal("Por ti los cojos danzan\nPor ti los mudos cantan", song.Sections[0].Text);
        Assert.Equal("Tu nombre exaltamos\nEres señor de la creación", song.Sections[1].Text);
        Assert.Equal("Marcos Witt", song.Artist);
        Assert.Equal("© 1999", song.Copyright);
    }

    [Fact]
    public void A_json_with_one_song_works_too()
    {
        var path = Write("una.json", """{ "title": "Sola", "artist": "Ella", "lyrics": { "paragraphs": [ { "text": "Hola" } ] } }""");

        var song = Assert.Single(HolyricsImporter.FromFile(path));

        Assert.Equal("Sola", song.Title);
    }

    [Fact]
    public void A_txt_keeps_title_artist_and_words()
    {
        var path = Write("Las Calles (Rescate).txt", "Título: Las Calles\nArtista: Rescate\n\nFotos de la vida.\n\nMucha gente grita.\n", bom: true);

        var song = Assert.Single(HolyricsImporter.FromFile(path));

        Assert.Equal("Las Calles", song.Title);
        Assert.Equal("Rescate", song.Artist);
        Assert.Equal(2, song.Sections.Count);
    }

    [Fact]
    public void A_txt_without_headers_takes_the_file_name_as_title()
    {
        var song = Assert.Single(HolyricsImporter.FromFile(Write("Sin cabecera.txt", "Primera línea\nSegunda línea")));

        Assert.Equal("Sin cabecera", song.Title);
        Assert.Contains("Primera línea", song.Sections[0].Text);
    }

    [Fact]
    public void A_file_that_is_not_json_says_so()
    {
        var path = Write("roto.json", "esto no es json");

        var ex = Assert.Throws<HolyricsException>(() => HolyricsImporter.FromFile(path));

        Assert.Contains("roto.json", ex.Message);
    }

    [Theory]
    [InlineData("Verse 3", "Estrofa 3")]
    [InlineData("Refrão", "Coro")]
    [InlineData("chorus 2", "Coro 2")]
    [InlineData("Pré-Refrão", "Pre-coro")]
    [InlineData("Pre-Chorus", "Pre-coro")]
    [InlineData("Ending", "Final")]
    [InlineData("Intro:", "Intro")]
    [InlineData("Coro", "Coro")]
    [InlineData("Puente", "Puente")]
    [InlineData("Alabanza [libre]", "Alabanza (libre)")]
    [InlineData("  ", "")]
    public void Translates_the_paragraph_descriptions(string description, string label) =>
        Assert.Equal(label, HolyricsImporter.Label(description));
}
