using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Songs.Web;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Finds a song on the internet and brings it into the library in one step,
/// instead of copying the lyrics from a browser and pasting them slide by
/// slide.
///
/// One box covers the three ways people remember a song: its name, who sings
/// it, or a line of the lyrics. musica.com is asked first, both through the
/// quick search (names) and its full search (lines of lyrics); LRCLIB fills in
/// for what musica.com does not have.
/// </summary>
public partial class WebSongSearchWindow : Window
{
    /// <summary>A result in the list.</summary>
    public sealed partial class Row(WebSongHit hit, int rank) : ObservableObject
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

    private const int QuickRank = 0;
    private const int FullRank = 1;
    private const int FallbackRank = 2;

    private readonly ISongRepository _songs;
    private readonly WebSongClient _client = new();
    private readonly ObservableCollection<Row> _results = [];
    private readonly Dictionary<string, WebSong> _fetched = [];
    private readonly HashSet<string> _libraryTitles;
    private readonly List<Song> _imported = [];
    private readonly HashSet<string> _importedUrls = [];

    private MusicaComBrowserSearch? _browserSearch;
    private CancellationTokenSource? _searchCancel;
    private CancellationTokenSource? _previewCancel;
    private Row? _previewRow;

    public WebSongSearchWindow(ISongRepository songs)
    {
        InitializeComponent();
        _songs = songs;
        ResultsList.ItemsSource = _results;

        _libraryTitles = new HashSet<string>(
            _songs.Search(string.Empty).Select(s => s.Title),
            StringComparer.OrdinalIgnoreCase);

        Loaded += (_, _) =>
        {
            if (WebViewProfile.IsRuntimeAvailable())
                _browserSearch = new MusicaComBrowserSearch(new WindowInteropHelper(this).Handle);
            QueryBox.Focus();
        };

        Closed += (_, _) =>
        {
            _searchCancel?.Cancel();
            _previewCancel?.Cancel();
            _browserSearch?.Dispose();
            _client.Dispose();
        };

        ShowEmptyPreview();
    }

    /// <summary>Songs added to the library, in the order they were imported.</summary>
    public IReadOnlyList<Song> Imported => _imported;

    // ── Búsqueda ─────────────────────────────────────────────────

    private void Query_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        Search();
    }

    private void Search_Click(object sender, RoutedEventArgs e) => Search();

    private async void Search()
    {
        var query = QueryBox.Text.Trim();
        if (query.Length < 2)
        {
            SearchStatusText.Text = "Escribí al menos un par de letras.";
            return;
        }

        _searchCancel?.Cancel();
        var cancel = (_searchCancel = new CancellationTokenSource()).Token;

        _results.Clear();
        ShowEmptyPreview();
        SearchStatusText.Text = "Buscando en musica.com…";

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
            Collect(QuickRank, "musica.com", () => _client.SearchMusicaComAsync(query, cancel)),
            Collect(FallbackRank, "lrclib.net", () => _client.SearchLrclibAsync(query, cancel)),
        };
        if (_browserSearch is { } browser)
            searches.Add(Collect(FullRank, "musica.com (por frase)", () => browser.SearchAsync(query, cancel)));

        await Task.WhenAll(searches);
        if (cancel.IsCancellationRequested)
            return;

        SearchStatusText.Text = _results.Count switch
        {
            0 when failures.Count == searches.Count =>
                "No me pude conectar. ¿Hay internet en esta computadora?",
            0 => "No encontré nada. Probá con otras palabras, o con una frase distinta de la letra.",
            1 => "1 resultado.",
            _ => $"{_results.Count} resultados.",
        };

        if (failures.Count > 0 && _results.Count > 0)
            SearchStatusText.Text += $" (No respondió: {string.Join(", ", failures)}.)";
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
            if (_results.Any(r => r.Hit.Url == hit.Url))
                continue;

            var index = 0;
            while (index < _results.Count && _results[index].Rank <= rank)
                index++;

            _results.Insert(index, new Row(hit, rank) { Imported = _importedUrls.Contains(hit.Url) });
        }

        if (_results.Count > 0)
        {
            SearchStatusText.Text = $"{_results.Count} resultados hasta ahora… sigo buscando.";
            if (ResultsList.SelectedItem is null)
                ResultsList.SelectedIndex = 0;
        }
    }

    // ── Vista previa ─────────────────────────────────────────────

    private async void Results_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not Row row || row == _previewRow)
            return;

        _previewCancel?.Cancel();
        var cancel = (_previewCancel = new CancellationTokenSource()).Token;

        _previewRow = row;
        ImportButton.IsEnabled = false;
        TitleBox.Text = row.Hit.Title;
        ArtistBox.Text = row.Hit.Artist;
        LyricsBox.Text = string.Empty;
        SourceLink.Visibility = Visibility.Visible;
        SourceLinkText.Text = $"Ver en {row.Hit.SourceName}";
        PreviewInfoText.Text = "Trayendo la letra…";

        WebSong? song;
        try
        {
            if (!_fetched.TryGetValue(row.Hit.Url, out song))
            {
                song = await _client.GetSongAsync(row.Hit, cancel);
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
            PreviewInfoText.Text = "No pude traer la letra. Revisá la conexión y volvé a elegirla.";
            _previewRow = null;
            return;
        }

        if (cancel.IsCancellationRequested)
            return;

        if (song is null)
        {
            PreviewInfoText.Text = "Esa página no tiene la letra. Probá con otro resultado.";
            return;
        }

        TitleBox.Text = song.Title;
        ArtistBox.Text = song.Artist;
        LyricsBox.Text = song.Lyrics;
        LyricsBox.ScrollToHome();
        UpdatePreviewInfo();
    }

    private void Preview_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_previewRow is not null && LyricsBox.Text.Length > 0)
            UpdatePreviewInfo();
    }

    private void UpdatePreviewInfo()
    {
        var slides = LyricsParser.Parse(LyricsBox.Text).Count;
        var title = TitleBox.Text.Trim();

        var info = slides == 1 ? "1 diapositiva." : $"{slides} diapositivas.";
        if (title.Length > 0 && _libraryTitles.Contains(title))
            info += $"  Ojo: ya tenés una canción llamada «{title}» en la biblioteca.";

        PreviewInfoText.Text = info;
        ImportButton.IsEnabled = slides > 0 && title.Length > 0;
    }

    private void ShowEmptyPreview()
    {
        _previewCancel?.Cancel();
        _previewRow = null;
        TitleBox.Text = string.Empty;
        ArtistBox.Text = string.Empty;
        LyricsBox.Text = string.Empty;
        SourceLink.Visibility = Visibility.Collapsed;
        PreviewInfoText.Text = "Elegí un resultado para ver la letra.";
        ImportButton.IsEnabled = false;
    }

    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if (_previewRow is null)
            return;

        // LRCLIB's link is the API; its site has no page per song worth opening.
        var url = _previewRow.Hit.Source == WebSongSource.Lrclib
            ? $"https://lrclib.net/search/{Uri.EscapeDataString(_previewRow.Hit.Title + " " + _previewRow.Hit.Artist)}"
            : _previewRow.Hit.Url;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true,
        });
    }

    // ── Importación ──────────────────────────────────────────────

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        var sections = LyricsParser.Parse(LyricsBox.Text);
        if (title.Length == 0 || sections.Count == 0)
            return;

        if (_libraryTitles.Contains(title))
        {
            var answer = MessageBox.Show(
                $"Ya tenés una canción llamada «{title}» en la biblioteca.\n\n¿La importo igual, como otra canción aparte?",
                "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;
        }

        try
        {
            var song = _songs.Save(new Song
            {
                Title = title,
                Artist = ArtistBox.Text.Trim(),
                Sections = sections,
            });

            _imported.Add(song);
            _libraryTitles.Add(title);
            if (_previewRow is not null)
            {
                _previewRow.Imported = true;
                _importedUrls.Add(_previewRow.Hit.Url);
            }

            Log.Information("Canción importada de {Url}: {Title}", _previewRow?.Hit.Url, title);
            StatusText.Text = _imported.Count == 1
                ? $"«{title}» ya está en tu biblioteca. Podés buscar otra o cerrar."
                : $"«{title}» ya está en tu biblioteca ({_imported.Count} importadas). Podés buscar otra o cerrar.";
            UpdatePreviewInfo();
            QueryBox.Focus();
            QueryBox.SelectAll();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo guardar la canción {Title}", title);
            MessageBox.Show("No se pudo guardar la canción. El detalle está en el registro de la aplicación.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
