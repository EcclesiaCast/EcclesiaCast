using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Data;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Brings songs over from another program (ProPresenter, EasyWorship or
/// Holyrics) without asking the operator to hunt for files: the operator picks
/// the program, the window looks where that one keeps its libraries, lists
/// what it found with the song count, and imports the ticked ones in one go.
/// Holyrics encrypts its library, so its songs come through the API Server it
/// documents: the operator gives the port and a token once, and they are kept.
/// </summary>
public partial class SongImportWindow : Window
{
    private enum Source { ProPresenter, EasyWorship, Holyrics }

    private const string HolyricsPortKey = "Import.Holyrics.Port";
    private const string HolyricsTokenKey = "Import.Holyrics.Token";

    /// <summary>One song to bring over; <paramref name="Path"/> is its file, when it has one.</summary>
    private sealed record Item(string Label, string? Path, Func<Song> Load);

    /// <summary>A library with its tick box, bound to the list.</summary>
    public sealed partial class Row(string title, string detail, int songCount, int cloudOnlyCount, Func<List<object>> items)
        : ObservableObject
    {
        public string Name => cloudOnlyCount > 0
            ? $"{title}  ·  {songCount} canción(es)  ·  ☁ {cloudOnlyCount} sin descargar"
            : $"{title}  ·  {songCount} canción(es)";

        public string Detail => detail;

        public int SongCount => songCount;

        public int CloudOnlyCount => cloudOnlyCount;

        /// <summary>Folder the library lives in, also what tells two libraries apart.</summary>
        public string Folder => detail;

        internal Func<List<object>> Items => items;

        [ObservableProperty]
        private bool _selected = songCount > 0;
    }

    private readonly SongSaver _songSaver;
    private readonly ISettingsStore _settings;
    private readonly Dictionary<Source, List<Row>> _rows = new()
    {
        [Source.ProPresenter] = [],
        [Source.EasyWorship] = [],
        [Source.Holyrics] = [],
    };
    private readonly Dictionary<Source, string> _roots = new()
    {
        [Source.ProPresenter] = string.Empty,
        [Source.EasyWorship] = string.Empty,
        [Source.Holyrics] = string.Empty,
    };

    /// <summary>Where Holyrics is installed, or null when it is not on this computer.</summary>
    private string? _holyricsFolder;

    /// <summary>Why the last attempt to reach Holyrics failed; empty when it did not.</summary>
    private string _holyricsError = string.Empty;

    private bool _connecting;
    private bool _importing;

    private Source _source = Source.ProPresenter;
    private bool _discovered;

    public SongImportWindow(SongSaver songSaver, ISettingsStore settings)
    {
        InitializeComponent();
        _songSaver = songSaver;
        _settings = settings;

        HolyricsPort.Text = settings.Get(HolyricsPortKey) is { Length: > 0 } port
            ? port
            : HolyricsClient.DefaultPort.ToString();
        HolyricsToken.Text = settings.Get(HolyricsTokenKey) ?? string.Empty;

        Loaded += (_, _) => DiscoverAll();
    }

    /// <summary>How many songs were actually added (0 if cancelled).</summary>
    public int Imported { get; private set; }

    /// <summary>Line for the operator's status bar.</summary>
    public string Summary { get; private set; } = string.Empty;

    private List<Row> Rows => _rows[_source];

    private string SourceName => _source switch
    {
        Source.ProPresenter => "ProPresenter",
        Source.EasyWorship => "EasyWorship",
        _ => "Holyrics",
    };

    // ── Descubrimiento ───────────────────────────────────────────

    /// <summary>Looks for both programs, so each choice says how much it found, and picks the one that has songs.</summary>
    private void DiscoverAll()
    {
        DiscoverProPresenter();
        DiscoverEasyWorship();
        _holyricsFolder = HolyricsLocator.FindInstall(HolyricsLocator.DefaultFolders());
        _discovered = true;

        RefreshLabels();

        // The program that has songs opens first; with several, ProPresenter as always.
        // Holyrics can't be counted before connecting, so it opens first only when it
        // is installed and the other two found nothing.
        var first = Total(Source.ProPresenter) > 0 ? Source.ProPresenter
            : Total(Source.EasyWorship) > 0 ? Source.EasyWorship
            : _holyricsFolder is not null ? Source.Holyrics
            : Source.ProPresenter;
        Choice(first).IsChecked = true;

        // With a token kept from a previous time, Holyrics is read right away.
        if (_holyricsFolder is not null && HolyricsToken.Text.Trim().Length > 0)
            _ = ConnectHolyricsAsync(quiet: true);
    }

    private System.Windows.Controls.RadioButton Choice(Source source) => source switch
    {
        Source.ProPresenter => ProPresenterChoice,
        Source.EasyWorship => EasyWorshipChoice,
        _ => HolyricsChoice,
    };

    private void RefreshLabels()
    {
        ProPresenterChoice.Content = Label("ProPresenter", Source.ProPresenter);
        EasyWorshipChoice.Content = Label("EasyWorship", Source.EasyWorship);
        HolyricsChoice.Content = Label("Holyrics", Source.Holyrics);
    }

    private int Total(Source source) => _rows[source].Sum(r => r.SongCount);

    private string Label(string name, Source source) =>
        Total(source) > 0 ? $"{name}  ·  {Total(source)}" : name;

    private void DiscoverProPresenter()
    {
        var roots = ProPresenterLocator.FindRoots(DocumentFolders());
        foreach (var root in roots)
        {
            foreach (var library in ProPresenterLocator.ReadLibraries(root))
                _rows[Source.ProPresenter].Add(ProPresenterRow(library));
        }

        _roots[Source.ProPresenter] = string.Join("\n", roots);
    }

    private void DiscoverEasyWorship()
    {
        var libraries = EasyWorshipDatabase.FindLibraries(SoftouchFolder);
        foreach (var folder in libraries)
            AddEasyWorship(folder);

        _roots[Source.EasyWorship] = string.Join("\n", libraries);
    }

    private static Row ProPresenterRow(ProPresenterLibrary library) => new(
        library.Name, library.Path, library.SongCount, library.CloudOnlyCount,
        () => library.SongFiles
            .Select(path => (object)new Item(
                Path.GetFileNameWithoutExtension(path), path,
                () => ProPresenterImporter.FromFile(Path.GetFileName(path), File.ReadAllBytes(path))))
            .ToList());

    private static string SoftouchFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Softouch");

    private void AddEasyWorship(string folder)
    {
        if (_rows[Source.EasyWorship].Any(r => string.Equals(r.Folder, folder, StringComparison.OrdinalIgnoreCase)))
            return;

        if (EasyWorshipDatabase.FindFiles(folder) is not { } files)
            return;

        // «Easyworship · Default»: the folder above the version one is the profile. The version
        // folder (v6.1) names the format of the data, not the program: EasyWorship 8 keeps its
        // songs in a folder called v6.1, so it is left out of the name.
        var profile = Directory.GetParent(folder)?.Parent?.Parent;
        var title = profile?.Parent is { } product ? $"{product.Name} · {profile.Name}" : Path.GetFileName(folder);

        _rows[Source.EasyWorship].Add(new Row(
            title, folder, EasyWorshipDatabase.CountSongs(files.Songs), 0,
            () =>
            {
                // The words are read here, when importing, not while listing.
                var songs = EasyWorshipDatabase.ReadSongs(files.Songs, files.Words, out var bad);
                var items = songs
                    .Select(song => (object)new Item(song.Title, null, () => song))
                    .ToList();
                for (var i = 0; i < bad; i++)
                    items.Add(new Item("(canción ilegible)", null, () => throw new InvalidDataException("Fila ilegible de EasyWorship")));
                return items;
            }));
    }

    private void Source_Checked(object sender, RoutedEventArgs e)
    {
        if (!_discovered)
            return;

        _source = ReferenceEquals(sender, EasyWorshipChoice) ? Source.EasyWorship
            : ReferenceEquals(sender, HolyricsChoice) ? Source.Holyrics
            : Source.ProPresenter;
        ShowSource();
    }

    /// <summary>Fills the window with what was found for the chosen program.</summary>
    private void ShowSource()
    {
        LibrariesList.ItemsSource = null;
        LibrariesList.ItemsSource = Rows;

        var holyrics = _source == Source.Holyrics;
        HolyricsPanel.Visibility = holyrics ? Visibility.Visible : Visibility.Collapsed;
        BrowseButton.Visibility = holyrics ? Visibility.Collapsed : Visibility.Visible;

        var total = Total(_source);
        if (holyrics)
        {
            ShowHolyrics(total);
        }
        else if (Rows.Count == 0)
        {
            HeaderText.Text = $"No encontré {SourceName} en esta computadora.";
            RootText.Text = _source == Source.ProPresenter
                ? "Si tus canciones están en otra carpeta (un disco externo, una copia de otra máquina), "
                  + "buscala con «Elegir carpeta…». Sirve tanto la carpeta ProPresenter entera como una "
                  + "biblioteca suelta llena de archivos .pro."
                : "Si tus canciones están en otra carpeta (una copia, otra computadora), buscala con "
                  + "«Elegir carpeta…»: la que tiene los archivos Songs.db y SongWords.db "
                  + "(en EasyWorship es «Databases\\Data»).";
            ImportButton.IsEnabled = false;
        }
        else
        {
            HeaderText.Text = total > 0
                ? $"Encontré {total} canción(es) en {Rows.Count} biblioteca(s) de {SourceName}. ¿Las importo todas?"
                : $"Encontré {SourceName}, pero sus bibliotecas están vacías.";
            RootText.Text = _roots[_source];
            ImportButton.IsEnabled = total > 0;
        }

        BrowseButton.ToolTip = _source == Source.ProPresenter
            ? "Si tus canciones están en otro lado (un disco externo, una copia), elegí la carpeta a mano"
            : "Si tus canciones están en otro lado (una copia, otra computadora), elegí la carpeta que tiene Songs.db y SongWords.db";
        ShowCloudWarning();
    }

    /// <summary>What the window says for Holyrics: the songs read, or how to connect.</summary>
    private void ShowHolyrics(int total)
    {
        if (Rows.Count > 0)
        {
            HeaderText.Text = total > 0
                ? $"Encontré {total} canción(es) en Holyrics. ¿Las importo todas?"
                : "Me conecté con Holyrics, pero no tiene canciones.";
            RootText.Text = _roots[Source.Holyrics];
            ImportButton.IsEnabled = total > 0 && !_importing;
            return;
        }

        HeaderText.Text = _connecting
            ? "Conectando con Holyrics…"
            : _holyricsError.Length > 0
                ? _holyricsError
                : _holyricsFolder is null
                    ? "No encontré Holyrics instalado en esta computadora."
                    : $"Encontré Holyrics en {_holyricsFolder}. Para traer sus canciones hay que conectarse con él.";
        RootText.Text =
            "Holyrics guarda sus canciones cifradas, así que se leen a través de su API Server:\n"
            + "1. Abrí Holyrics y entrá en Archivo → Configuraciones → API Server.\n"
            + "2. Activá el servidor local y fijate el puerto (suele ser 8091).\n"
            + "3. En los permisos, creá un token y habilitale «GetSongs» y «GetLyrics».\n"
            + "4. Escribí el puerto y el token acá abajo y tocá «Conectar», con Holyrics abierto.\n"
            + "El token queda guardado: la próxima vez se conecta solo.";
        ImportButton.IsEnabled = false;
    }

    /// <summary>
    /// Warns when the songs are OneDrive placeholders. Without this the import
    /// looks like it works — the files are listed, the progress bar runs — and
    /// every single read fails, which is exactly what happened the first time
    /// this was used for real. Only ProPresenter's are plain files in a folder.
    /// </summary>
    private void ShowCloudWarning()
    {
        var cloudOnly = _source == Source.ProPresenter ? Rows.Sum(r => r.CloudOnlyCount) : 0;
        if (cloudOnly == 0)
        {
            CloudWarning.Visibility = Visibility.Collapsed;
            return;
        }

        CloudWarning.Visibility = Visibility.Visible;
        CloudWarningText.Text =
            $"{cloudOnly} de esas canciones están en OneDrive y todavía no bajaron a esta computadora. "
            + "Así como están no se pueden leer.\n\n"
            + "Abrí la carpeta en el Explorador, hacé clic derecho sobre ella y elegí "
            + "«Conservar siempre en este dispositivo». Esperá a que termine de bajar "
            + "(el ícono pasa de nube ☁ a tilde verde ✓) y volvé a intentar.";
    }

    /// <summary>
    /// Where to look for ProPresenter: Documents and the OneDrive mirror of it,
    /// in both the English and Spanish spellings — with OneDrive on, the real
    /// folder usually lives there and not in the local Documents.
    /// </summary>
    private static IEnumerable<string> DocumentFolders()
    {
        var folders = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documentos"),
        };

        foreach (var oneDrive in new[]
                 {
                     Environment.GetEnvironmentVariable("OneDrive"),
                     Environment.GetEnvironmentVariable("OneDriveConsumer"),
                     Environment.GetEnvironmentVariable("OneDriveCommercial"),
                 })
        {
            if (string.IsNullOrWhiteSpace(oneDrive))
                continue;
            folders.Add(Path.Combine(oneDrive, "Documents"));
            folders.Add(Path.Combine(oneDrive, "Documentos"));
        }

        return folders;
    }

    private async void HolyricsConnect_Click(object sender, RoutedEventArgs e) =>
        await ConnectHolyricsAsync(quiet: false);

    /// <summary>
    /// Reads every song of Holyrics through its API and lists them as one
    /// library. <paramref name="quiet"/> is the attempt made on opening with a
    /// kept token: if Holyrics is closed then, the window just shows the steps
    /// and waits for «Conectar» instead of greeting the operator with an error.
    /// </summary>
    private async Task ConnectHolyricsAsync(bool quiet)
    {
        if (_connecting || _importing)
            return;

        var token = HolyricsToken.Text.Trim();
        if (!int.TryParse(HolyricsPort.Text.Trim(), out var port) || port is < 1 or > 65535)
        {
            _holyricsError = "El puerto tiene que ser un número, como 8091.";
            ShowSource();
            return;
        }
        if (token.Length == 0)
        {
            _holyricsError = "Falta el token: crealo en Holyrics, en Archivo → Configuraciones → API Server.";
            ShowSource();
            return;
        }

        _connecting = true;
        _holyricsError = string.Empty;
        HolyricsConnect.IsEnabled = false;
        if (_source == Source.Holyrics)
            ShowSource();

        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(quiet ? 5 : 60) };
            var library = await new HolyricsClient(http, port, token).ReadSongsAsync();

            _settings.Set(HolyricsPortKey, port.ToString());
            _settings.Set(HolyricsTokenKey, token);

            var detail = $"API Server de Holyrics, puerto {port}";
            if (library.Archived > 0)
                detail += $"  ·  {library.Archived} archivada(s) en Holyrics, no se traen";
            if (library.Unreadable > 0)
                detail += $"  ·  {library.Unreadable} ilegible(s)";

            var songs = library.Songs;
            _rows[Source.Holyrics].Clear();
            _rows[Source.Holyrics].Add(new Row(
                "Holyrics", detail, songs.Count, 0,
                () => songs.Select(song => (object)new Item(song.Title, null, () => song)).ToList()));
            _roots[Source.Holyrics] = detail;

            Log.Information("Holyrics: {Count} canciones leídas, {Archived} archivadas, {Bad} ilegibles",
                songs.Count, library.Archived, library.Unreadable);
        }
        catch (HolyricsException ex)
        {
            Log.Warning(ex, "No se pudo leer Holyrics");
            // On opening, a closed Holyrics is not worth an error: just the steps.
            _holyricsError = quiet ? string.Empty : ex.Message;
        }
        finally
        {
            _connecting = false;
            HolyricsConnect.IsEnabled = !_importing;
        }

        RefreshLabels();
        if (_source == Source.Holyrics)
            ShowSource();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = _source == Source.ProPresenter
                ? "Carpeta de ProPresenter (o una biblioteca suelta)"
                : "Carpeta de EasyWorship con Songs.db y SongWords.db (Databases\\Data)",
        };
        if (dialog.ShowDialog() != true)
            return;

        var folder = dialog.FolderName;
        var added = _source == Source.ProPresenter ? BrowseProPresenter(folder) : BrowseEasyWorship(folder);
        if (!added)
            return;

        RefreshLabels();
        _roots[_source] = folder;
        ShowSource();
        HeaderText.Text = $"{Total(_source)} canción(es) en {Rows.Count} biblioteca(s). ¿Las importo todas?";
    }

    /// <summary>Either the ProPresenter root (which holds Libraries) or one library.</summary>
    private bool BrowseProPresenter(string folder)
    {
        var libraries = ProPresenterLocator.ReadLibraries(folder);
        if (libraries.Count == 0)
        {
            var single = ProPresenterLocator.ReadFolder(folder);
            if (single.SongCount == 0)
            {
                Notice.Show("En esa carpeta no hay archivos .pro de ProPresenter.");
                return false;
            }
            libraries = [single];
        }

        foreach (var library in libraries)
        {
            if (Rows.Any(r => string.Equals(r.Folder, library.Path, StringComparison.OrdinalIgnoreCase)))
                continue;
            Rows.Add(ProPresenterRow(library));
        }

        return true;
    }

    private bool BrowseEasyWorship(string folder)
    {
        if (EasyWorshipDatabase.FindFiles(folder) is null)
        {
            Notice.Show("En esa carpeta no están los archivos Songs.db y SongWords.db de EasyWorship.");
            return false;
        }

        AddEasyWorship(folder);
        return true;
    }

    /// <summary>Opens the first library that has undownloaded songs.</summary>
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = Rows.FirstOrDefault(r => r.CloudOnlyCount > 0)?.Folder ?? Rows.FirstOrDefault()?.Folder;
        if (folder is null || !Directory.Exists(folder))
            return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true,
        });
    }

    private void CheckAll_Click(object sender, RoutedEventArgs e) => SetAll(true);

    private void CheckNone_Click(object sender, RoutedEventArgs e) => SetAll(false);

    private void SetAll(bool selected)
    {
        foreach (var row in Rows)
            row.Selected = selected && row.SongCount > 0;
    }

    // ── Importación ──────────────────────────────────────────────

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var chosen = Rows.Where(r => r.Selected).ToList();
        if (chosen.Count == 0)
        {
            Notice.Show("Marcá al menos una biblioteca.");
            return;
        }

        _importing = true;
        ImportButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        BrowseButton.IsEnabled = false;
        ProPresenterChoice.IsEnabled = false;
        EasyWorshipChoice.IsEnabled = false;
        HolyricsChoice.IsEnabled = false;
        HolyricsConnect.IsEnabled = false;
        Progress.Visibility = Visibility.Visible;
        ProgressText.Visibility = Visibility.Visible;
        ProgressText.Text = $"Leyendo las bibliotecas de {SourceName}…";
        Progress.IsIndeterminate = true;

        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

        var items = new List<Item>();
        var unreadableLibraries = 0;
        foreach (var row in chosen)
        {
            try
            {
                items.AddRange(row.Items().Cast<Item>());
            }
            catch (Exception ex) when (ex is InvalidDataException or Microsoft.Data.Sqlite.SqliteException or IOException)
            {
                Log.Error(ex, "No se pudo leer la biblioteca de {Source} {Path}", SourceName, row.Folder);
                unreadableLibraries++;
            }
        }

        Progress.IsIndeterminate = false;
        Progress.Maximum = Math.Max(1, items.Count);

        // Titles are matched in memory by the saver: asking the database once
        // per song turns a 300-song import into 300 round trips. A repeated
        // title is asked about, with a box to answer the same for the rest.
        var session = _songSaver.Begin(several: true);

        int imported = 0, replaced = 0, skipped = 0, discarded = 0, failed = unreadableLibraries, cloudOnly = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            Progress.Value = i + 1;
            ProgressText.Text = $"Importando {i + 1} de {items.Count}: {item.Label}";

            // Let the window repaint between songs; this runs on the UI thread
            // because the repository isn't safe to hit from another one.
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

            try
            {
                var song = item.Load();

                if (song.Sections.Count == 0)
                {
                    Log.Warning("{Source} sin texto: {Label}", SourceName, item.Path ?? item.Label);
                    skipped++;
                    continue;
                }

                switch (session.Save(song).Result)
                {
                    case SongSaveResult.Replaced: replaced++; break;
                    case SongSaveResult.Discarded: discarded++; break;
                    default: imported++; break;
                }
            }
            catch (IOException ex) when (item.Path is not null && ProPresenterLocator.IsCloudOnly(item.Path))
            {
                // The file is a OneDrive placeholder and nothing downloaded it.
                Log.Warning(ex, "Canción sólo en la nube: {Path}", item.Path);
                cloudOnly++;
                failed++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falló la importación de {Label}", item.Path ?? item.Label);
                failed++;
            }
        }

        Imported = imported + replaced;

        var parts = new List<string> { $"{imported} canciones importadas de {SourceName}" };
        if (replaced > 0)
            parts.Add($"{replaced} reemplazadas");
        if (discarded > 0)
            parts.Add($"{discarded} descartadas por repetidas");
        if (skipped > 0)
            parts.Add($"{skipped} omitidas (vacías)");
        if (failed > 0)
            parts.Add($"{failed} con error (ver el log)");
        Summary = string.Join(" · ", parts) + ".";

        Log.Information(
            "Importación de {Source}: {Imported} importadas, {Replaced} reemplazadas, {Discarded} descartadas, {Skipped} omitidas, {Failed} con error ({Cloud} sólo en la nube)",
            SourceName, imported, replaced, discarded, skipped, failed, cloudOnly);

        // Nothing came through: say so out loud instead of leaving a line in
        // the status bar that is easy to miss and looks like success.
        if (imported + replaced == 0)
        {
            var reason = cloudOnly > 0
                ? $"Las {cloudOnly} canciones están en OneDrive y todavía no bajaron a esta computadora.\n\n"
                  + "Abrí la carpeta en el Explorador, clic derecho sobre ella → «Conservar siempre en "
                  + "este dispositivo», esperá a que termine de bajar y volvé a intentar."
                : discarded > 0 && discarded + skipped == items.Count
                    ? "Todas ya estaban en tu biblioteca y las descartaste."
                    : "No se pudo leer ninguna. El detalle está en el registro de la aplicación.";

            Notice.Show(reason, "No se importó ninguna canción", NoticeKind.Warning);
        }

        DialogResult = true;
    }
}
