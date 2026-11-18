using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Songs.Web;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class WebSongSearchViewModelTests
{
    private sealed class Rig : IDisposable
    {
        public ViewModelWorld World { get; } = new();

        public FakeWebSongs Web { get; } = new();

        public ScriptedPrompt Prompt { get; }

        public WebSongSearchViewModel Vm { get; }

        public List<(string Title, string Artist, string Lyrics, bool Top)> Texts { get; } = [];

        public List<WebSongRow> SelectAsked { get; } = [];

        public int ImportsDone { get; private set; }

        public Rig(int promptChoice = 0, Action<ViewModelWorld>? seed = null)
        {
            seed?.Invoke(World);
            Prompt = new ScriptedPrompt(promptChoice);
            Vm = new WebSongSearchViewModel(new SongSaver(World.Songs, Prompt), World.Songs, Web);
            Vm.PreviewTextChanged += (t, a, l, top) => Texts.Add((t, a, l, top));
            Vm.SelectRequested += SelectAsked.Add;
            Vm.ImportDone += () => ImportsDone++;
        }

        public void Dispose() => World.Dispose();
    }

    private static WebSongHit Quick(string title, string artist, string url) =>
        new(WebSongSource.MusicaCom, title, artist, url);

    private static WebSongHit Lrclib(string title, string artist, string url, string lyrics) =>
        new(WebSongSource.Lrclib, title, artist, url, lyrics);

    private const string Lyrics = "Primera línea\nSegunda línea\n\nCoro\nCantemos todos";

    [Fact]
    public async Task A_query_of_less_than_two_letters_only_says_so()
    {
        using var rig = new Rig();

        await rig.Vm.SearchAsync(" a ");

        Assert.Equal("Escribí al menos un par de letras.", rig.Vm.SearchStatus);
        Assert.Empty(rig.Web.Queries);
    }

    [Fact]
    public async Task The_results_come_in_the_order_of_their_source_and_without_repeats()
    {
        using var rig = new Rig();
        rig.Web.Quick.Add(Quick("Grande es Dios", "Coro", "https://m/1"));
        rig.Web.Fallback.Add(Lrclib("Grande es Dios", "Otro", "https://m/1", Lyrics)); // same address: left out
        rig.Web.Fallback.Add(Lrclib("Grande es Él", "Ana", "https://l/2", Lyrics));

        await rig.Vm.SearchAsync("grande");

        Assert.Equal(["https://m/1", "https://l/2"], rig.Vm.Results.Select(r => r.Hit.Url));
        Assert.Equal("2 resultados.", rig.Vm.SearchStatus);
        Assert.Equal("Coro · musica.com", rig.Vm.Results[0].Detail);
        Assert.Equal("Ana · lrclib.net", rig.Vm.Results[1].Detail);
        Assert.Equal(["grande"], rig.Web.Queries);
    }

    [Fact]
    public async Task The_search_by_a_line_of_the_lyrics_goes_between_the_quick_one_and_the_fallback()
    {
        using var rig = new Rig();
        rig.Web.Quick.Add(Quick("A", "x", "https://m/a"));
        rig.Web.Fallback.Add(Lrclib("C", "z", "https://l/c", Lyrics));
        string? asked = null;
        rig.Vm.BrowserSearch = (query, _) =>
        {
            asked = query;
            return Task.FromResult<IReadOnlyList<WebSongHit>>([Quick("B", "y", "https://m/b")]);
        };

        await rig.Vm.SearchAsync("  una frase  ");

        Assert.Equal("una frase", asked);
        Assert.Equal(["https://m/a", "https://m/b", "https://l/c"], rig.Vm.Results.Select(r => r.Hit.Url));
    }

    [Fact]
    public async Task Nothing_found_says_so()
    {
        using var rig = new Rig();

        await rig.Vm.SearchAsync("zzzz");

        Assert.Equal("No encontré nada. Probá con otras palabras, o con una frase distinta de la letra.",
            rig.Vm.SearchStatus);
    }

    [Fact]
    public async Task When_every_source_fails_it_says_there_may_be_no_internet()
    {
        using var rig = new Rig();
        rig.Web.QuickFails = true;
        rig.Web.FallbackFails = true;

        await rig.Vm.SearchAsync("grande");

        Assert.Equal("No me pude conectar. ¿Hay internet en esta computadora?", rig.Vm.SearchStatus);
    }

    [Fact]
    public async Task When_one_source_fails_the_others_still_show_and_the_failure_is_named()
    {
        using var rig = new Rig();
        rig.Web.QuickFails = true;
        rig.Web.Fallback.Add(Lrclib("Grande es Él", "Ana", "https://l/2", Lyrics));

        await rig.Vm.SearchAsync("grande");

        Assert.Equal("1 resultado. (No respondió: musica.com.)", rig.Vm.SearchStatus);
    }

    [Fact]
    public async Task The_first_result_is_asked_to_be_chosen_as_results_arrive()
    {
        using var rig = new Rig();
        rig.Web.Quick.Add(Quick("A", "x", "https://m/a"));

        await rig.Vm.SearchAsync("grande");

        Assert.NotEmpty(rig.SelectAsked);
        Assert.Same(rig.Vm.Results[0], rig.SelectAsked[0]);
    }

    [Fact]
    public async Task A_new_search_empties_the_list_and_the_preview()
    {
        using var rig = new Rig();
        rig.Web.Quick.Add(Quick("A", "x", "https://m/a"));
        await rig.Vm.SearchAsync("uno");
        rig.Texts.Clear();

        rig.Web.Quick.Clear();
        await rig.Vm.SearchAsync("dos");

        Assert.Empty(rig.Vm.Results);
        Assert.Equal(("", "", "", false), rig.Texts[0]);
        Assert.Equal("Elegí un resultado para ver la letra.", rig.Vm.PreviewInfo);
        Assert.False(rig.Vm.CanImport);
        Assert.False(rig.Vm.SourceLinkVisible);
    }

    [Fact]
    public void Before_anything_is_chosen_the_preview_asks_to_choose()
    {
        using var rig = new Rig();

        Assert.Equal("Elegí un resultado para ver la letra.", rig.Vm.PreviewInfo);
        Assert.False(rig.Vm.CanImport);
        Assert.False(rig.Vm.SourceLinkVisible);
        Assert.Null(rig.Vm.SourceUrl);
    }

    [Fact]
    public async Task Choosing_a_result_with_its_lyrics_brings_them_and_counts_the_slides()
    {
        using var rig = new Rig();
        var row = new WebSongRow(Lrclib("Grande es Él", "Ana", "https://l/2", Lyrics), 2);
        rig.Web.Pages["https://l/2"] = new WebSong("Grande es Él", "Ana", Lyrics);

        await rig.Vm.ShowPreviewAsync(row);

        Assert.Equal(("Grande es Él", "Ana", "", false), rig.Texts[^2]);
        Assert.Equal(("Grande es Él", "Ana", Lyrics, true), rig.Texts[^1]);
        Assert.Equal("2 diapositivas.", rig.Vm.PreviewInfo);
        Assert.True(rig.Vm.CanImport);
        Assert.True(rig.Vm.SourceLinkVisible);
        Assert.Equal("Ver en lrclib.net", rig.Vm.SourceLinkText);
    }

    [Fact]
    public async Task Choosing_the_same_result_again_does_not_ask_the_page_again()
    {
        using var rig = new Rig();
        var row = new WebSongRow(Quick("A", "x", "https://m/a"), 0);
        rig.Web.Pages["https://m/a"] = new WebSong("A", "x", Lyrics);

        await rig.Vm.ShowPreviewAsync(row);
        await rig.Vm.ShowPreviewAsync(row);
        await rig.Vm.ShowPreviewAsync(new WebSongRow(Quick("A", "x", "https://m/a"), 0));

        // The second time is the same row (ignored); a new row for the same address uses what was read.
        Assert.Equal(["https://m/a"], rig.Web.PagesAsked);
    }

    [Fact]
    public async Task A_page_without_lyrics_says_so_and_nothing_can_be_imported()
    {
        using var rig = new Rig();
        var row = new WebSongRow(Quick("A", "x", "https://m/a"), 0);
        rig.Web.Pages["https://m/a"] = null;

        await rig.Vm.ShowPreviewAsync(row);

        Assert.Equal("Esa página no tiene la letra. Probá con otro resultado.", rig.Vm.PreviewInfo);
        Assert.False(rig.Vm.CanImport);
    }

    [Fact]
    public async Task When_the_page_cannot_be_read_it_says_so_and_the_same_result_can_be_chosen_again()
    {
        using var rig = new Rig();
        var row = new WebSongRow(Quick("A", "x", "https://m/a"), 0);
        rig.Web.PagesFail = true;

        await rig.Vm.ShowPreviewAsync(row);

        Assert.Equal("No pude traer la letra. Revisá la conexión y volvé a elegirla.", rig.Vm.PreviewInfo);
        Assert.Null(rig.Vm.SourceUrl);

        rig.Web.PagesFail = false;
        rig.Web.Pages["https://m/a"] = new WebSong("A", "x", Lyrics);
        await rig.Vm.ShowPreviewAsync(row);

        Assert.Equal(2, rig.Web.PagesAsked.Count);
        Assert.True(rig.Vm.CanImport);
    }

    [Fact]
    public async Task A_title_already_in_the_library_is_warned_about_in_the_preview()
    {
        using var rig = new Rig(seed: w => w.Songs.Save(new Song
        {
            Title = "Grande es Él",
            Sections = [new SongSection { Order = 0, Label = "Verso", Text = "x" }],
        }));
        var row = new WebSongRow(Quick("Grande es Él", "", "https://m/a"), 0);
        rig.Web.Pages["https://m/a"] = new WebSong("Grande es Él", "", Lyrics);

        await rig.Vm.ShowPreviewAsync(row);

        Assert.Equal(
            "2 diapositivas.  Ojo: ya tenés una canción llamada «Grande es Él» en la biblioteca; al importar te pregunto qué hacer.",
            rig.Vm.PreviewInfo);
    }

    [Fact]
    public async Task Editing_the_texts_refreshes_the_info_and_the_button_only_with_a_result_chosen_and_lyrics()
    {
        using var rig = new Rig();
        rig.Vm.PreviewEdited("Algo", "", Lyrics);
        Assert.Equal("Elegí un resultado para ver la letra.", rig.Vm.PreviewInfo);

        var row = new WebSongRow(Quick("A", "x", "https://m/a"), 0);
        rig.Web.Pages["https://m/a"] = new WebSong("A", "x", Lyrics);
        await rig.Vm.ShowPreviewAsync(row);

        rig.Vm.PreviewEdited("A", "x", "Una sola línea");
        Assert.Equal("1 diapositiva.", rig.Vm.PreviewInfo);
        Assert.True(rig.Vm.CanImport);

        rig.Vm.PreviewEdited("   ", "x", "Una sola línea");
        Assert.False(rig.Vm.CanImport);

        // With no lyrics the info is left as it was.
        var before = rig.Vm.PreviewInfo;
        rig.Vm.PreviewEdited("A", "x", "");
        Assert.Equal(before, rig.Vm.PreviewInfo);
    }

    [Fact]
    public async Task The_source_link_goes_to_the_page_or_to_the_search_of_lrclib()
    {
        using var rig = new Rig();
        rig.Web.Pages["https://m/a"] = new WebSong("A", "x", Lyrics);
        rig.Web.Pages["https://l/2"] = new WebSong("Grande es Él", "Ana", Lyrics);

        await rig.Vm.ShowPreviewAsync(new WebSongRow(Quick("A", "x", "https://m/a"), 0));
        Assert.Equal("https://m/a", rig.Vm.SourceUrl);

        await rig.Vm.ShowPreviewAsync(new WebSongRow(Lrclib("Grande es Él", "Ana", "https://l/2", Lyrics), 2));
        Assert.Equal("https://lrclib.net/search/Grande%20es%20%C3%89l%20Ana", rig.Vm.SourceUrl);
    }

    [Fact]
    public async Task Importing_saves_the_song_marks_the_result_and_says_so()
    {
        using var rig = new Rig();
        var row = new WebSongRow(Quick("A", "x", "https://m/a"), 0);
        rig.Web.Pages["https://m/a"] = new WebSong("A", "x", Lyrics);
        await rig.Vm.ShowPreviewAsync(row);

        rig.Vm.Import(" Grande es Él ", " Ana ", Lyrics);

        var song = Assert.Single(rig.Vm.Imported);
        Assert.Equal("Grande es Él", song.Title);
        Assert.Equal("Ana", song.Artist);
        Assert.Equal(2, rig.World.Songs.Get(song.Id)!.Sections.Count);
        Assert.True(row.Imported);
        Assert.Equal("«Grande es Él» ya está en tu biblioteca. Podés buscar otra o cerrar.", rig.Vm.Status);
        Assert.Equal(1, rig.ImportsDone);
        Assert.Empty(rig.Prompt.Messages);
    }

    [Fact]
    public async Task A_second_import_counts_them_and_a_result_already_imported_is_marked_in_a_new_search()
    {
        using var rig = new Rig();
        var row = new WebSongRow(Quick("A", "x", "https://m/a"), 0);
        rig.Web.Pages["https://m/a"] = new WebSong("A", "x", Lyrics);
        await rig.Vm.ShowPreviewAsync(row);
        rig.Vm.Import("Uno", "", Lyrics);
        rig.Vm.Import("Dos", "", Lyrics);

        Assert.Equal("«Dos» ya está en tu biblioteca (2 importadas). Podés buscar otra o cerrar.", rig.Vm.Status);

        rig.Web.Quick.Add(Quick("A", "x", "https://m/a"));
        await rig.Vm.SearchAsync("a ver");

        Assert.True(rig.Vm.Results[0].Imported);
    }

    [Fact]
    public async Task Importing_without_a_title_or_without_lyrics_does_nothing()
    {
        using var rig = new Rig();
        await Task.CompletedTask;

        rig.Vm.Import("   ", "x", Lyrics);
        rig.Vm.Import("Algo", "x", "   ");

        Assert.Empty(rig.Vm.Imported);
        Assert.Empty(rig.World.Songs.Search());
        Assert.Equal(0, rig.ImportsDone);
    }

    [Fact]
    public void A_repeated_title_asks_what_to_do_and_a_discard_says_it_was_not_imported()
    {
        using var rig = new Rig(promptChoice: 2, seed: w => w.Songs.Save(new Song
        {
            Title = "Grande es Él",
            Sections = [new SongSection { Order = 0, Label = "Verso", Text = "x" }],
        }));

        rig.Vm.Import("Grande es Él", "", Lyrics);

        Assert.Single(rig.Prompt.Messages);
        Assert.Empty(rig.Vm.Imported);
        Assert.Equal("«Grande es Él» no se importó: ya estaba en tu biblioteca.", rig.Vm.Status);
        Assert.Equal(0, rig.ImportsDone);
        Assert.Single(rig.World.Songs.Search());
    }

    [Fact]
    public void A_repeated_title_can_be_kept_as_a_copy()
    {
        using var rig = new Rig(promptChoice: 0, seed: w => w.Songs.Save(new Song
        {
            Title = "Grande es Él",
            Sections = [new SongSection { Order = 0, Label = "Verso", Text = "x" }],
        }));

        rig.Vm.Import("Grande es Él", "", Lyrics);

        Assert.Equal("Grande es Él (1)", Assert.Single(rig.Vm.Imported).Title);
        Assert.Equal(2, rig.World.Songs.Search().Count);
    }

    [Fact]
    public void Closing_lets_go_of_the_connection()
    {
        using var rig = new Rig();

        rig.Vm.Dispose();

        Assert.True(rig.Web.Disposed);
    }
}
