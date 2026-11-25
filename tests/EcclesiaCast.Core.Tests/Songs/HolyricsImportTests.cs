using System.Net;
using System.Text;
using System.Text.Json;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

/// <summary>
/// Holyrics is read through its API Server; these answers are written by hand
/// after the shapes documented at github.com/holyrics/API-Server.
/// </summary>
public class HolyricsImportTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ecclesia-holyrics-" + Guid.NewGuid().ToString("N"));

    public HolyricsImportTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private const string Song = """
        {
          "id": "1795171747799",
          "title": "Cuán grande es Él",
          "artist": "Stuart K. Hine",
          "author": "Carl Boberg",
          "copyright": "© 1949 Stuart K. Hine",
          "archived": false,
          "slides": [
            { "text": "Señor mi Dios, al contemplar los cielos,\r\nel firmamento y las estrellas mil,", "slide_description": "Verse 1" },
            { "text": "al oír tu voz en los potentes truenos\ny ver brillar al sol en su cenit,", "slide_description": "" },
            { "text": "Mi corazón entona la canción:\n¡Cuán grande es Él!", "slide_description": "Chorus" },
            { "text": "", "slide_description": "Verse 2" },
            { "text": "Al recorrer los montes y los valles", "slide_description": "Estrofe 2" },
            { "text": "Cuando el Señor me llame a su presencia", "slide_description": "Ponte" }
          ],
          "order": "1,2,3,5,3,6,3"
        }
        """;

    private static Song Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return HolyricsImporter.FromJson(document.RootElement);
    }

    [Fact]
    public void Reads_title_artist_copyright_with_accents()
    {
        var song = Parse(Song);

        Assert.Equal("Cuán grande es Él", song.Title);
        Assert.Equal("Stuart K. Hine", song.Artist);
        Assert.Equal("© 1949 Stuart K. Hine", song.Copyright);
    }

    [Fact]
    public void Each_slide_is_a_section_labelled_in_spanish()
    {
        var song = Parse(Song);

        Assert.Equal(
            ["Estrofa 1", "Estrofa 1", "Coro", "Estrofa 2", "Puente"],
            song.Sections.Select(s => s.Label));
        Assert.Equal("Señor mi Dios, al contemplar los cielos,\nel firmamento y las estrellas mil,", song.Sections[0].Text);
        Assert.Equal("Mi corazón entona la canción:\n¡Cuán grande es Él!", song.Sections[2].Text);
        Assert.Equal(Enumerable.Range(0, 5), song.Sections.Select(s => s.Order));
    }

    [Fact]
    public void Falls_back_to_the_author_and_a_title_when_missing()
    {
        var song = Parse("""{ "title": " ", "artist": "", "author": "Marcos Witt", "slides": [ { "text": "Hola" } ] }""");

        Assert.Equal("Sin título", song.Title);
        Assert.Equal("Marcos Witt", song.Artist);
        Assert.Null(song.Copyright);
        Assert.Equal("1", Assert.Single(song.Sections).Label);
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
    public void Translates_the_slide_descriptions(string description, string label) =>
        Assert.Equal(label, HolyricsImporter.Label(description));

    // ── The API ─────────────────────────────────────────────────

    private sealed class FakeHolyrics(Func<string, string, (HttpStatusCode, string)> answer) : HttpMessageHandler
    {
        public List<string> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Calls.Add(request.Method + " " + request.RequestUri!.AbsoluteUri + " " + body);
            var (status, json) = answer(request.RequestUri!.AbsolutePath, body);
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    private static Task<HolyricsLibrary> Read(FakeHolyrics handler, string token = "abc 123") =>
        new HolyricsClient(new HttpClient(handler), 8091, token).ReadSongsAsync();

    [Fact]
    public async Task Reads_every_song_in_one_call_and_leaves_archived_ones()
    {
        var handler = new FakeHolyrics((_, _) => (HttpStatusCode.OK, $$"""
            { "status": "ok", "data": [
                {{Song}},
                { "id": "2", "title": "Guardada", "archived": true, "slides": [ { "text": "x" } ] }
            ] }
            """));

        var library = await Read(handler);

        var song = Assert.Single(library.Songs);
        Assert.Equal("Cuán grande es Él", song.Title);
        Assert.Equal(1, library.Archived);
        Assert.Equal(0, library.Unreadable);

        var call = Assert.Single(handler.Calls);
        Assert.StartsWith("POST http://127.0.0.1:8091/api/GetSongs?token=abc%20123 ", call);
        Assert.Contains("\"fields\":\"id,title,artist,author,copyright,archived,slides\"", call);
    }

    [Fact]
    public async Task Asks_song_by_song_when_the_list_comes_without_words()
    {
        var handler = new FakeHolyrics((path, body) => path switch
        {
            "/api/GetSongs" => (HttpStatusCode.OK, """{ "status": "ok", "data": [ { "id": "7", "title": "Corta" }, { "title": "Sin id" } ] }"""),
            "/api/GetLyrics" when body.Contains("\"id\":\"7\"") => (HttpStatusCode.OK,
                """{ "status": "ok", "data": { "id": "7", "title": "Corta", "slides": [ { "text": "Aleluya", "slide_description": "Coro" } ] } }"""),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        var library = await Read(handler);

        var song = Assert.Single(library.Songs);
        Assert.Equal("Coro", Assert.Single(song.Sections).Label);
        Assert.Equal(1, library.Unreadable);
        Assert.Equal(2, handler.Calls.Count);
    }

    [Fact]
    public async Task A_wrong_token_says_so_in_spanish()
    {
        var handler = new FakeHolyrics((_, _) => (HttpStatusCode.OK, """{ "status": "error", "error": "invalid token" }"""));

        var ex = await Assert.ThrowsAsync<HolyricsException>(() => Read(handler));

        Assert.Contains("no reconoce ese token", ex.Message);
    }

    [Fact]
    public async Task A_token_without_permission_says_which_ones_to_enable()
    {
        var handler = new FakeHolyrics((_, _) => (HttpStatusCode.Unauthorized, "{}"));

        var ex = await Assert.ThrowsAsync<HolyricsException>(() => Read(handler));

        Assert.Contains("GetSongs", ex.Message);
    }

    [Fact]
    public async Task Something_else_on_the_port_is_reported()
    {
        var handler = new FakeHolyrics((_, _) => (HttpStatusCode.OK, "<html>no</html>"));

        var ex = await Assert.ThrowsAsync<HolyricsException>(() => Read(handler));

        Assert.Contains("puerto 8091", ex.Message);
    }

    [Fact]
    public async Task Holyrics_closed_explains_how_to_turn_the_server_on()
    {
        var client = new HolyricsClient(new HttpClient(new Refusing()), 8091, "t");

        var ex = await Assert.ThrowsAsync<HolyricsException>(() => client.ReadSongsAsync());

        Assert.Contains("API Server", ex.Message);
    }

    private sealed class Refusing : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }

    // ── Finding it ──────────────────────────────────────────────

    [Fact]
    public void Finds_the_folder_with_Holyrics_exe()
    {
        var empty = Path.Combine(_folder, "nada");
        var install = Path.Combine(_folder, "Holyrics");
        Directory.CreateDirectory(empty);
        Directory.CreateDirectory(install);
        File.WriteAllText(Path.Combine(install, "Holyrics.exe"), "");

        Assert.Equal(install, HolyricsLocator.FindInstall([null, "", empty, install]));
        Assert.Null(HolyricsLocator.FindInstall([empty]));
    }
}
