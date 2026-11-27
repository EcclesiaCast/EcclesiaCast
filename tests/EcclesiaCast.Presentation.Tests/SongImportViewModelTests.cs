using EcclesiaCast.Core.Songs;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using Microsoft.Data.Sqlite;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class SongImportViewModelTests : IDisposable
{
    private readonly ViewModelWorld _world = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ecclesiacast-import-" + Guid.NewGuid().ToString("N"));
    private readonly TempPaths _paths;

    public SongImportViewModelTests()
    {
        Directory.CreateDirectory(_root);
        _paths = new TempPaths(_root);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        _world.Dispose();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A temporary folder left behind is not worth failing a test.
        }
    }

    private SongImportViewModel Make(int promptChoice = 0) =>
        new(new SongSaver(_world.Songs, new ScriptedPrompt(promptChoice)), _paths);

    private string ProLibrary(string library, params (string File, string? Text)[] songs)
    {
        var folder = Path.Combine(_paths.DocumentFolders[0], "ProPresenter", "Libraries", library);
        Directory.CreateDirectory(folder);
        foreach (var (file, text) in songs)
        {
            // The text of a slide lives in the file as RTF; a file without any has no words.
            var content = text is null ? "x" : @"{\rtf1\ansi " + text + "}";
            File.WriteAllText(Path.Combine(folder, file), content);
        }

        return folder;
    }

    private static void Exec(string folder, string file, params string[] statements)
    {
        using var connection = new SqliteConnection($"Data Source={Path.Combine(folder, file)};Pooling=False");
        connection.Open();
        foreach (var sql in statements)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>An EasyWorship library with one song with words; returns its data folder.</summary>
    private string EasyWorshipLibrary(string root, string product = "Easyworship")
    {
        var folder = Path.Combine(root, product, "Default", "v6.1", "Databases", "Data");
        Directory.CreateDirectory(folder);
        Exec(folder, "Songs.db",
            "CREATE TABLE song (title TEXT, author TEXT, copyright TEXT, description TEXT)",
            "INSERT INTO song (title, author, copyright) VALUES ('Ardiendo en fuego', 'Autor', NULL)");
        Exec(folder, "SongWords.db", "CREATE TABLE word (song_id INTEGER, words TEXT)",
            @"INSERT INTO word (song_id, words) VALUES (1, '{\rtf1\ansi\deff0{\fonttbl{\f0 Arial;}}\pard Verse 1\par Linea uno\par Linea dos\par}')");
        return folder;
    }

    private static Task Repaint() => Task.CompletedTask;

    // ── Discovery ────────────────────────────────────────────────

    [Fact]
    public void Finds_the_libraries_of_propresenter_where_the_platform_says_to_look()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola"), ("Otra.pro", "Chau"));
        ProLibrary("Biblia", ("Salmo.pro", "Salmo"));
        var vm = Make();

        var first = vm.DiscoverAll();
        vm.SelectSource(first);

        Assert.Equal(SongImportSource.ProPresenter, first);
        Assert.Equal(["Biblia", "Canciones"], vm.Rows.Select(r => Path.GetFileName(r.Folder)));
        Assert.Equal("ProPresenter  ·  3", vm.ChoiceLabel(SongImportSource.ProPresenter));
        Assert.Equal("EasyWorship", vm.ChoiceLabel(SongImportSource.EasyWorship));
        Assert.Equal("Encontré 3 canción(es) en 2 biblioteca(s) de ProPresenter. ¿Las importo todas?", vm.HeaderText);
        Assert.True(vm.CanImport);
        Assert.False(vm.CloudWarningVisible);
        Assert.All(vm.Rows, r => Assert.True(r.Selected));
    }

    [Fact]
    public void Finds_easyworship_under_its_softouch_folder_and_opens_it_when_it_is_the_only_one()
    {
        EasyWorshipLibrary(_paths.SoftouchFolder);
        var vm = Make();

        var first = vm.DiscoverAll();
        vm.SelectSource(first);

        Assert.Equal(SongImportSource.EasyWorship, first);
        var row = Assert.Single(vm.Rows);
        Assert.Equal("Easyworship · Default", row.Name.Split("  ·  ")[0]);
        Assert.Equal(1, row.SongCount);
        Assert.Equal("EasyWorship  ·  1", vm.ChoiceLabel(SongImportSource.EasyWorship));
        Assert.Equal("Encontré 1 canción(es) en 1 biblioteca(s) de EasyWorship. ¿Las importo todas?", vm.HeaderText);
    }

    [Fact]
    public void With_songs_in_both_propresenter_opens_first()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola"));
        EasyWorshipLibrary(_paths.SoftouchFolder);

        Assert.Equal(SongImportSource.ProPresenter, Make().DiscoverAll());
    }

    [Fact]
    public void Without_anything_found_it_says_so_and_nothing_can_be_imported()
    {
        var vm = Make();
        vm.DiscoverAll();

        vm.SelectSource(SongImportSource.ProPresenter);
        Assert.Equal("No encontré ProPresenter en esta computadora.", vm.HeaderText);
        Assert.Contains("«Elegir carpeta…»", vm.RootText);
        Assert.False(vm.CanImport);

        vm.SelectSource(SongImportSource.EasyWorship);
        Assert.Equal("No encontré EasyWorship en esta computadora.", vm.HeaderText);
        Assert.Contains("Songs.db y SongWords.db", vm.RootText);
        Assert.Equal("Carpeta de EasyWorship con Songs.db y SongWords.db (Databases\\Data)", vm.BrowseTitle);
    }

    [Fact]
    public void Choosing_a_program_before_looking_does_nothing()
    {
        var vm = Make();

        vm.SelectSource(SongImportSource.EasyWorship);

        Assert.False(vm.Discovered);
        Assert.Equal(SongImportSource.ProPresenter, vm.Source);
        Assert.Equal("Buscando…", vm.HeaderText);
    }

    [Fact]
    public void Libraries_that_are_empty_are_listed_but_cannot_be_imported()
    {
        ProLibrary("Vacía");
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());

        Assert.Equal("Encontré ProPresenter, pero sus bibliotecas están vacías.", vm.HeaderText);
        Assert.False(vm.CanImport);
        Assert.False(vm.Rows[0].Selected);
    }

    [Fact]
    public void The_folders_it_looks_in_are_the_ones_of_the_platform_not_the_real_documents()
    {
        // The only place with a ProPresenter folder is the temporary one the paths point to.
        var vm = Make();
        vm.DiscoverAll();
        vm.SelectSource(SongImportSource.ProPresenter);

        Assert.Empty(vm.Rows);
        Assert.StartsWith("Si tus canciones están en otra carpeta", vm.RootText);
    }

    [Fact]
    public void The_folder_where_it_looked_is_shown_under_the_header()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola"));
        var vm = Make();

        vm.SelectSource(vm.DiscoverAll());

        Assert.Equal(Path.Combine(_paths.DocumentFolders[0], "ProPresenter"), vm.RootText);
    }

    // ── A folder chosen by hand ──────────────────────────────────

    [Fact]
    public void A_folder_without_pro_files_says_so()
    {
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        using var notices = new ScriptedNotices();
        var empty = Path.Combine(_root, "vacia");
        Directory.CreateDirectory(empty);

        var added = vm.Browse(empty);

        Assert.False(added);
        Assert.Equal("En esa carpeta no hay archivos .pro de ProPresenter.", notices.Asked[0].Message);
    }

    [Fact]
    public void A_loose_library_chosen_by_hand_is_added_once()
    {
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        var loose = Path.Combine(_root, "suelta");
        Directory.CreateDirectory(loose);
        File.WriteAllText(Path.Combine(loose, "Una.pro"), @"{\rtf1\ansi Hola}");

        Assert.True(vm.Browse(loose));
        Assert.True(vm.Browse(loose));

        Assert.Single(vm.Rows);
        Assert.Equal("1 canción(es) en 1 biblioteca(s). ¿Las importo todas?", vm.HeaderText);
        Assert.Equal(loose, vm.RootText);
        Assert.True(vm.CanImport);
        Assert.Equal("ProPresenter  ·  1", vm.ChoiceLabel(SongImportSource.ProPresenter));
    }

    [Fact]
    public void A_whole_propresenter_folder_chosen_by_hand_adds_its_libraries()
    {
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        var root = Path.Combine(_root, "disco", "ProPresenter");
        Directory.CreateDirectory(Path.Combine(root, "Libraries", "A"));
        Directory.CreateDirectory(Path.Combine(root, "Libraries", "B"));
        File.WriteAllText(Path.Combine(root, "Libraries", "A", "Una.pro"), @"{\rtf1\ansi Hola}");

        Assert.True(vm.Browse(root));

        Assert.Equal(2, vm.Rows.Count);
    }

    [Fact]
    public void An_easyworship_folder_without_its_files_says_so_and_with_them_is_added()
    {
        var vm = Make();
        vm.DiscoverAll();
        vm.SelectSource(SongImportSource.EasyWorship);
        using var notices = new ScriptedNotices();
        var empty = Path.Combine(_root, "vacia");
        Directory.CreateDirectory(empty);

        Assert.False(vm.Browse(empty));
        Assert.Equal("En esa carpeta no están los archivos Songs.db y SongWords.db de EasyWorship.", notices.Asked[0].Message);

        var data = EasyWorshipLibrary(Path.Combine(_root, "copia"));
        Assert.True(vm.Browse(data));
        Assert.Single(vm.Rows);
        Assert.Equal(data, vm.RootText);
    }

    [Fact]
    public void The_folder_to_open_is_the_first_library_if_it_exists()
    {
        var folder = ProLibrary("Canciones", ("Una.pro", "Hola"));
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());

        Assert.Equal(folder, vm.FolderToOpen());
    }

    [Fact]
    public void There_is_no_folder_to_open_without_libraries()
    {
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());

        Assert.Null(vm.FolderToOpen());
    }

    [Fact]
    public void Ticking_all_or_none_leaves_the_empty_libraries_unticked()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola"));
        ProLibrary("Vacía");
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());

        vm.SetAll(false);
        Assert.All(vm.Rows, r => Assert.False(r.Selected));

        vm.SetAll(true);
        Assert.Equal(["Canciones"], vm.Rows.Where(r => r.Selected).Select(r => Path.GetFileName(r.Folder)));
    }

    // ── Holyrics ─────────────────────────────────────────────────

    [Fact]
    public async Task Holyrics_files_are_listed_song_by_song_and_only_the_ticked_ones_come_over()
    {
        var file = Path.Combine(_root, "lote.json");
        File.WriteAllText(file, """
            [
              { "title": "Las Calles", "artist": "Rescate", "lyrics": { "paragraphs": [ { "text": "Fotos de la vida." } ] } },
              { "title": "Sola", "artist": "Ella", "lyrics": { "paragraphs": [ { "text": "Hola" } ] } }
            ]
            """);
        var broken = Path.Combine(_root, "roto.json");
        File.WriteAllText(broken, "{ esto no es");
        var vm = Make();
        vm.DiscoverAll();
        vm.SelectSource(SongImportSource.Holyrics);

        vm.ReadHolyricsFiles([file, broken]);

        Assert.Equal(["Las Calles", "Sola"], vm.Rows.Select(r => r.Name));
        Assert.True(vm.CanImport);
        Assert.Equal("CANCIONES ENCONTRADAS", vm.ListTitle);

        vm.Rows[1].Selected = false;
        Assert.True(await vm.ImportAsync(Repaint));

        Assert.Equal("Las Calles", Assert.Single(_world.Songs.Search()).Title);
    }

    [Fact]
    public void A_file_with_no_songs_of_holyrics_says_so()
    {
        var broken = Path.Combine(_root, "roto.json");
        File.WriteAllText(broken, "{ esto no es");
        var vm = Make();
        vm.DiscoverAll();
        vm.SelectSource(SongImportSource.Holyrics);

        vm.ReadHolyricsFiles([broken]);

        Assert.Empty(vm.Rows);
        Assert.False(vm.CanImport);
    }

    // ── Import ───────────────────────────────────────────────────

    [Fact]
    public async Task Importing_with_no_library_ticked_says_so_and_does_not_finish()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola"));
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        vm.SetAll(false);
        using var notices = new ScriptedNotices();

        var finished = await vm.ImportAsync(Repaint);

        Assert.False(finished);
        Assert.Equal("Marcá al menos una biblioteca.", notices.Asked[0].Message);
        Assert.False(vm.IsImporting);
        Assert.Empty(_world.Songs.Search());
    }

    [Fact]
    public async Task While_importing_it_cannot_be_closed_or_started_again()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola a todos"));
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        Assert.True(vm.ImportEnabled);
        Assert.True(vm.CanClose);
        var during = new List<(bool Import, bool Close)>();

        await vm.ImportAsync(() =>
        {
            during.Add((vm.ImportEnabled, vm.CanClose));
            return Task.CompletedTask;
        });

        Assert.All(during, state => Assert.Equal((false, false), state));
        // Finished: the window can go.
        Assert.True(vm.CanClose);
    }

    [Theory]
    [InlineData(PlatformSystem.Windows, "el Explorador")]
    [InlineData(PlatformSystem.Mac, "el Finder")]
    public void It_calls_the_window_of_a_folder_what_the_system_calls_it(PlatformSystem system, string name)
    {
        var vm = new SongImportViewModel(
            new SongSaver(_world.Songs, new ScriptedPrompt(0)), _paths, system);

        Assert.Equal($"Abrir la carpeta en {name}", vm.OpenFolderText);
    }

    [Fact]
    public void The_dialog_of_holyrics_asks_for_its_files()
    {
        var vm = new SongImportViewModel(
            new SongSaver(_world.Songs, new ScriptedPrompt(0)), _paths, PlatformSystem.Mac);

        var request = vm.HolyricsFilesRequest();

        Assert.Equal("Canciones guardadas desde Holyrics", request.Title);
        Assert.Equal(["*.json", "*.txt"], Assert.Single(request.Filters).Patterns);
        Assert.Equal(_paths.DocumentFolders[0], request.InitialDirectory);
        vm.SelectSource(vm.DiscoverAll());
        Assert.False(vm.IsHolyrics);
        vm.SelectSource(SongImportSource.Holyrics);
        Assert.True(vm.IsHolyrics);
    }

    [Fact]
    public async Task Importing_brings_the_songs_with_text_and_skips_the_empty_ones()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola a todos"), ("Vacia.pro", null));
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        var repaints = 0;

        var finished = await vm.ImportAsync(() =>
        {
            repaints++;
            return Task.CompletedTask;
        });

        Assert.True(finished);
        Assert.Equal(1, vm.Imported);
        Assert.Equal("1 canciones importadas de ProPresenter · 1 omitidas (vacías).", vm.Summary);
        var song = Assert.Single(_world.Songs.Search());
        Assert.Equal("Una", song.Title);
        // Once before reading and once before each song.
        Assert.Equal(3, repaints);
        Assert.True(vm.IsImporting);
        Assert.Equal(2, vm.ProgressMaximum);
        Assert.Equal(2, vm.ProgressValue);
        Assert.False(vm.ProgressIndeterminate);
        Assert.Equal("Importando 2 de 2: Vacia", vm.ProgressText);
    }

    [Fact]
    public async Task Importing_from_easyworship_reads_the_words_when_importing()
    {
        EasyWorshipLibrary(_paths.SoftouchFolder);
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());

        await vm.ImportAsync(Repaint);

        Assert.Equal("1 canciones importadas de EasyWorship.", vm.Summary);
        var song = _world.Songs.Get(Assert.Single(_world.Songs.Search()).Id)!;
        Assert.Equal("Ardiendo en fuego", song.Title);
        Assert.Equal("Autor", song.Artist);
        Assert.Equal("Linea uno\nLinea dos", song.Sections[0].Text);
    }

    [Fact]
    public async Task A_repeated_title_is_asked_about_and_a_discard_is_counted_and_said_out_loud()
    {
        _world.Songs.Save(new Song
        {
            Title = "Una",
            Sections = [new SongSection { Order = 0, Label = "1", Text = "x" }],
        });
        ProLibrary("Canciones", ("Una.pro", "Hola"));
        var vm = Make(promptChoice: 2);
        vm.SelectSource(vm.DiscoverAll());
        using var notices = new ScriptedNotices();

        await vm.ImportAsync(Repaint);

        Assert.Equal(0, vm.Imported);
        Assert.Equal("0 canciones importadas de ProPresenter · 1 descartadas por repetidas.", vm.Summary);
        var said = Assert.Single(notices.Asked);
        Assert.Equal("Todas ya estaban en tu biblioteca y las descartaste.", said.Message);
        Assert.Equal("No se importó ninguna canción", said.Title);
        Assert.Single(_world.Songs.Search());
    }

    [Fact]
    public async Task A_repeated_title_can_replace_the_song_that_is_there()
    {
        _world.Songs.Save(new Song
        {
            Title = "Una",
            Sections = [new SongSection { Order = 0, Label = "1", Text = "vieja" }],
        });
        ProLibrary("Canciones", ("Una.pro", "nueva"));
        var vm = Make(promptChoice: 1);
        vm.SelectSource(vm.DiscoverAll());

        await vm.ImportAsync(Repaint);

        Assert.Equal(1, vm.Imported);
        Assert.Equal("0 canciones importadas de ProPresenter · 1 reemplazadas.", vm.Summary);
        var song = _world.Songs.Get(Assert.Single(_world.Songs.Search()).Id)!;
        Assert.Equal("nueva", song.Sections[0].Text);
    }

    [Fact]
    public async Task When_every_song_is_empty_it_says_none_could_be_read()
    {
        ProLibrary("Canciones", ("Vacia.pro", null));
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        using var notices = new ScriptedNotices();

        await vm.ImportAsync(Repaint);

        Assert.Equal("0 canciones importadas de ProPresenter · 1 omitidas (vacías).", vm.Summary);
        Assert.Equal("No se pudo leer ninguna. El detalle está en el registro de la aplicación.", notices.Asked[0].Message);
        Assert.Equal(["Entendido"], notices.Asked[0].Buttons);
    }

    [Fact]
    public async Task Only_the_ticked_libraries_are_imported()
    {
        ProLibrary("Canciones", ("Una.pro", "Hola"));
        ProLibrary("Otra", ("Dos.pro", "Chau"));
        var vm = Make();
        vm.SelectSource(vm.DiscoverAll());
        vm.Rows.Single(r => Path.GetFileName(r.Folder) == "Otra").Selected = false;

        await vm.ImportAsync(Repaint);

        Assert.Equal(["Una"], _world.Songs.Search().Select(s => s.Title));
    }
}
