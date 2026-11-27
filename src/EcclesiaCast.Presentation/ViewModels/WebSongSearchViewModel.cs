using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Songs.Web;
using Serilog;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>A result in the list.</summary>
public sealed partial class WebSongRow(WebSongHit hit, int rank) : ObservableObject
{
    public WebSongHit Hit { get; } = hit;

    /// <summary>Order of the source in the list: musica.com first, the fallback last.</summary>
    public int Rank { get; } = rank;

    public string Detail => Hit.Artist.Length > 0
        ? $"{Hit.Artist} · {Hit.SourceName}"
        : Hit.SourceName;

    [ObservableProperty]
    private bool _imported;
}

/// <summary>
/// Backs the window that finds a song on the internet and brings it into the
/// library in one step, instead of copying the lyrics from a browser and
/// pasting them slide by slide.
///
/// One box covers the three ways people remember a song: its name, who sings
/// it, or a line of the lyrics. musica.com is asked first, both through the
/// quick search (names) and its full search (lines of lyrics, which runs in a
/// browser the window owns: see <see cref="BrowserSearch"/>); LRCLIB fills in
/// for what musica.com does not have.
///
/// The three text fields (title, artist, lyrics) belong to the operator while
/// they edit them, so the window hands their text over when it asks for
/// something that depends on it.
/// </summary>
public sealed partial class WebSongSearchViewModel : ObservableObject, IDisposable
{
    private const int QuickRank = 0;
    private const int FullRank = 1;
    private const int FallbackRank = 2;

    private readonly SongSaver _songSaver;
    private readonly IWebSongSource _source;
    private readonly Dictionary<string, WebSong> _fetched = [];
    private readonly List<SongTitle> _libraryTitles;
    private readonly List<Song> _imported = [];
    private readonly HashSet<string> _importedUrls = [];

    private CancellationTokenSource? _searchCancel;
    private CancellationTokenSource? _previewCancel;
    private WebSongRow? _previewRow;

    public WebSongSearchViewModel(SongSaver songSaver, ISongRepository songs, IWebSongSource source)
    {
        _songSaver = songSaver;
        _source = source;
        _libraryTitles = songs.GetTitles().ToList();
        ClearPreview();
    }

    public ObservableCollection<WebSongRow> Results { get; } = [];

    /// <summary>
    /// Whether the window tells the operator a line of the lyrics can be
    /// searched. False where there is no browser for it (the Avalonia
    /// program): without it the sources look for names, not for lines.
    /// </summary>
    public bool PhraseSearchPromised { get; init; } = true;

    /// <summary>The line under the search box.</summary>
    public string Hint => PhraseSearchPromised
        ? "Escribí el nombre, el artista o una frase de la letra y apretá Enter. Busca en musica.com y, como respaldo, en lrclib.net."
        : "Escribí el nombre o el artista y apretá Enter. Busca en musica.com y, como respaldo, en lrclib.net.";

    /// <summary>
    /// The search by a line of the lyrics, which only a browser can do: the
    /// window sets it when it has one. Null when there is none.
    /// </summary>
    public Func<string, CancellationToken, Task<IReadOnlyList<WebSongHit>>>? BrowserSearch { get; set; }

    /// <summary>Songs added to the library, in the order they were imported.</summary>
    public IReadOnlyList<Song> Imported => _imported;

    [ObservableProperty]
    private string _searchStatus = string.Empty;

    /// <summary>The line at the bottom, about what was imported.</summary>
    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _previewInfo = string.Empty;

    [ObservableProperty]
    private bool _canImport;

    [ObservableProperty]
    private bool _sourceLinkVisible;

    [ObservableProperty]
    private string _sourceLinkText = string.Empty;

    /// <summary>
    /// The fields of the preview get new text (the title, the artist and the
    /// lyrics; the last one is true when the lyrics are the song's own, so the
    /// box goes back to its top).
    /// </summary>
    public event Action<string, string, string, bool>? PreviewTextChanged;

    /// <summary>The first result is chosen by the window when nothing was chosen yet.</summary>
    public event Action<WebSongRow>? SelectRequested;

    /// <summary>The song was imported: the window goes back to the search box.</summary>
    public event Action? ImportDone;

    // ── Search ───────────────────────────────────────────────────

    public async Task SearchAsync(string queryText)
    {
        var query = queryText.Trim();
        if (query.Length < 2)
        {
            SearchStatus = "Escribí al menos un par de letras.";
            return;
        }

        _searchCancel?.Cancel();
        var cancel = (_searchCancel = new CancellationTokenSource()).Token;

        Results.Clear();
        ClearPreview();
        SearchStatus = "Buscando en musica.com…";

        var failures = new List<string>();

        async Task Collect(int rank, string source, Func<Task<IReadOnlyList<WebSongHit>>> search)
        {
            try
            {
                var hits = await search();
                if (!cancel.IsCancellationRequested)
                    AddResults(rank, hits);
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Falló la búsqueda de canciones en {Source} ({Query})", source, query);
                failures.Add(source);
            }
        }

        var searches = new List<Task>
        {
            Collect(QuickRank, "musica.com", () => _source.SearchMusicaComAsync(query, cancel)),
            Collect(FallbackRank, "lrclib.net", () => _source.SearchLrclibAsync(query, cancel)),
        };
        if (BrowserSearch is { } browser)
            searches.Add(Collect(FullRank, "musica.com (por frase)", () => browser(query, cancel)));

        await Task.WhenAll(searches);
        if (cancel.IsCancellationRequested)
            return;

        SearchStatus = Results.Count switch
        {
            0 when failures.Count == searches.Count =>
                "No me pude conectar. ¿Hay internet en esta computadora?",
            0 => PhraseSearchPromised
                ? "No encontré nada. Probá con otras palabras, o con una frase distinta de la letra."
                : "No encontré nada. Probá con el nombre de la canción o con quién la canta.",
            1 => "1 resultado.",
            _ => $"{Results.Count} resultados.",
        };

        if (failures.Count > 0 && Results.Count > 0)
            SearchStatus += $" (No respondió: {string.Join(", ", failures)}.)";
    }

    /// <summary>
    /// Adds a source's results in its place in the list as soon as they
    /// arrive: the quick search answers in a moment, the full one takes a few
    /// seconds, and there is no reason to make the operator wait for both.
    /// </summary>
    private void AddResults(int rank, IReadOnlyList<WebSongHit> hits)
    {
        foreach (var hit in hits)
        {
            if (Results.Any(r => r.Hit.Url == hit.Url))
                continue;

            var index = 0;
            while (index < Results.Count && Results[index].Rank <= rank)
                index++;

            Results.Insert(index, new WebSongRow(hit, rank) { Imported = _importedUrls.Contains(hit.Url) });
        }

        if (Results.Count > 0)
        {
            SearchStatus = $"{Results.Count} resultados hasta ahora… sigo buscando.";
            SelectRequested?.Invoke(Results[0]);
        }
    }

    // ── Preview ──────────────────────────────────────────────────

    /// <summary>A result was chosen in the list: brings its lyrics into the preview.</summary>
    public async Task ShowPreviewAsync(WebSongRow row)
    {
        if (row == _previewRow)
            return;

        _previewCancel?.Cancel();
        var cancel = (_previewCancel = new CancellationTokenSource()).Token;

        _previewRow = row;
        CanImport = false;
        PreviewTextChanged?.Invoke(row.Hit.Title, row.Hit.Artist, string.Empty, false);
        SourceLinkVisible = true;
        SourceLinkText = $"Ver en {row.Hit.SourceName}";
        PreviewInfo = "Trayendo la letra…";

        WebSong? song;
        try
        {
            if (!_fetched.TryGetValue(row.Hit.Url, out song))
            {
                song = await _source.GetSongAsync(row.Hit, cancel);
                if (song is not null)
                    _fetched[row.Hit.Url] = song;
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            // Mostly no connection or a timeout; nothing here is worth crashing over.
            Log.Warning(ex, "No se pudo traer la letra de {Url}", row.Hit.Url);
            PreviewInfo = "No pude traer la letra. Revisá la conexión y volvé a elegirla.";
            _previewRow = null;
            return;
        }

        if (cancel.IsCancellationRequested)
            return;

        if (song is null)
        {
            PreviewInfo = "Esa página no tiene la letra. Probá con otro resultado.";
            return;
        }

        PreviewTextChanged?.Invoke(song.Title, song.Artist, song.Lyrics, true);
        UpdatePreviewInfo(song.Title, song.Artist, song.Lyrics);
    }

    /// <summary>The operator edited the title or the lyrics.</summary>
    public void PreviewEdited(string title, string artist, string lyrics)
    {
        if (_previewRow is not null && lyrics.Length > 0)
            UpdatePreviewInfo(title, artist, lyrics);
    }

    private void UpdatePreviewInfo(string titleText, string artistText, string lyrics)
    {
        var slides = LyricsParser.Parse(lyrics).Count;
        var title = titleText.Trim();

        var info = slides == 1 ? "1 diapositiva." : $"{slides} diapositivas.";
        var artist = artistText.Trim();
        if (title.Length > 0 && SongTitles.FindSame(_libraryTitles, title, artist) is not null)
            info += $"  Ojo: ya tenés una canción llamada «{title}» en la biblioteca; al importar te pregunto qué hacer.";

        PreviewInfo = info;
        CanImport = slides > 0 && title.Length > 0;
    }

    /// <summary>Empties the preview (a new search, or nothing chosen yet).</summary>
    public void ClearPreview()
    {
        _previewCancel?.Cancel();
        _previewRow = null;
        PreviewTextChanged?.Invoke(string.Empty, string.Empty, string.Empty, false);
        SourceLinkVisible = false;
        PreviewInfo = "Elegí un resultado para ver la letra.";
        CanImport = false;
    }

    /// <summary>
    /// The address «Ver en…» opens, or null when no result is chosen. LRCLIB's
    /// own address is the API; its site has no page per song worth opening, so
    /// it goes to its search.
    /// </summary>
    public string? SourceUrl => _previewRow is null
        ? null
        : _previewRow.Hit.Source == WebSongSource.Lrclib
            ? $"https://lrclib.net/search/{Uri.EscapeDataString(_previewRow.Hit.Title + " " + _previewRow.Hit.Artist)}"
            : _previewRow.Hit.Url;

    // ── Import ───────────────────────────────────────────────────

    /// <summary>
    /// Brings the song as the operator left it into the library. A repeated
    /// title is asked about: replace, keep both or drop it.
    /// </summary>
    public void Import(string titleText, string artistText, string lyrics)
    {
        var title = titleText.Trim();
        var sections = LyricsParser.Parse(lyrics);
        if (title.Length == 0 || sections.Count == 0)
            return;

        try
        {
            var outcome = _songSaver.Begin().Save(new Song
            {
                Title = title,
                Artist = artistText.Trim(),
                Sections = sections,
            });

            if (outcome.Song is not { } song)
            {
                Status = $"«{title}» no se importó: ya estaba en tu biblioteca.";
                return;
            }

            title = song.Title;
            _imported.RemoveAll(s => s.Id == song.Id);
            _imported.Add(song);
            _libraryTitles.Add(new SongTitle(song.Id, title, song.Artist));
            if (_previewRow is not null)
            {
                _previewRow.Imported = true;
                _importedUrls.Add(_previewRow.Hit.Url);
            }

            Log.Information("Canción importada de {Url}: {Title}", _previewRow?.Hit.Url, title);
            Status = _imported.Count == 1
                ? $"«{title}» ya está en tu biblioteca. Podés buscar otra o cerrar."
                : $"«{title}» ya está en tu biblioteca ({_imported.Count} importadas). Podés buscar otra o cerrar.";
            UpdatePreviewInfo(titleText, artistText, lyrics);
            ImportDone?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo guardar la canción {Title}", title);
            Notice.Show("El detalle está en el registro de la aplicación.",
                "No se pudo guardar la canción", NoticeKind.Error);
        }
    }

    /// <summary>The window closed: what was in the middle stops.</summary>
    public void Dispose()
    {
        _searchCancel?.Cancel();
        _previewCancel?.Cancel();
        _source.Dispose();
    }
}
