using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Data;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Brings songs over from EasyWorship without asking the operator to hunt for
/// files: it looks where EasyWorship keeps its libraries, lists what it found
/// with the song count, and imports the ticked ones in one go.
/// </summary>
public partial class EasyWorshipImportWindow : Window
{
    /// <summary>A library (a folder with Songs.db and SongWords.db) with its tick box.</summary>
    public sealed partial class Row(string folder, int songCount) : ObservableObject
    {
        public string Folder { get; } = folder;

        public int SongCount { get; } = songCount;

        /// <summary>«Easyworship  v6.1», read from the folders above Databases\Data.</summary>
        public string Name
        {
            get
            {
                var version = Directory.GetParent(Folder)?.Parent;
                var product = version?.Parent?.Parent;
                var label = product is null || version is null ? Path.GetFileName(Folder) : $"{product.Name} {version.Name}";
                return $"{label}  ·  {SongCount} canción(es)";
            }
        }

        public string Detail => Folder;

        [ObservableProperty]
        private bool _selected = songCount > 0;
    }

    private readonly SongSaver _songSaver;
    private readonly List<Row> _rows = [];

    public EasyWorshipImportWindow(SongSaver songSaver)
    {
        InitializeComponent();
        _songSaver = songSaver;
        LibrariesList.ItemsSource = _rows;

        Loaded += (_, _) => Discover();
    }

    /// <summary>How many songs were actually added (0 if cancelled).</summary>
    public int Imported { get; private set; }

    /// <summary>Line for the operator's status bar.</summary>
    public string Summary { get; private set; } = string.Empty;

    // ── Descubrimiento ───────────────────────────────────────────

    private static string SoftouchFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Softouch");

    private void Discover()
    {
        _rows.Clear();

        foreach (var folder in EasyWorshipDatabase.FindLibraries(SoftouchFolder))
            AddLibrary(folder);

        LibrariesList.Items.Refresh();

        var total = _rows.Sum(r => r.SongCount);
        if (_rows.Count == 0)
        {
            HeaderText.Text = "No encontré EasyWorship en esta computadora.";
            RootText.Text = "Si tus canciones están en otra carpeta (una copia, otra computadora), buscala con "
                          + "«Elegir carpeta…»: la que tiene los archivos Songs.db y SongWords.db "
                          + "(en EasyWorship es «Databases\\Data»).";
            ImportButton.IsEnabled = false;
            return;
        }

        HeaderText.Text = total > 0
            ? $"Encontré {total} canción(es) en {_rows.Count} biblioteca(s) de EasyWorship. ¿Las importo todas?"
            : "Encontré EasyWorship, pero sus bibliotecas están vacías.";
        RootText.Text = string.Join("\n", _rows.Select(r => r.Folder));
        ImportButton.IsEnabled = total > 0;
    }

    private void AddLibrary(string folder)
    {
        if (_rows.Any(r => string.Equals(r.Folder, folder, StringComparison.OrdinalIgnoreCase)))
            return;

        if (EasyWorshipDatabase.FindFiles(folder) is { } files)
            _rows.Add(new Row(folder, EasyWorshipDatabase.CountSongs(files.Songs)));
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Carpeta de EasyWorship con Songs.db y SongWords.db (Databases\\Data)",
        };
        if (dialog.ShowDialog() != true)
            return;

        if (EasyWorshipDatabase.FindFiles(dialog.FolderName) is null)
        {
            MessageBox.Show(
                "En esa carpeta no están los archivos Songs.db y SongWords.db de EasyWorship.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        AddLibrary(dialog.FolderName);
        LibrariesList.Items.Refresh();

        var total = _rows.Sum(r => r.SongCount);
        HeaderText.Text = $"{total} canción(es) en {_rows.Count} biblioteca(s). ¿Las importo todas?";
        RootText.Text = dialog.FolderName;
        ImportButton.IsEnabled = total > 0;
    }

    private void CheckAll_Click(object sender, RoutedEventArgs e) => SetAll(true);

    private void CheckNone_Click(object sender, RoutedEventArgs e) => SetAll(false);

    private void SetAll(bool selected)
    {
        foreach (var row in _rows)
            row.Selected = selected && row.SongCount > 0;
    }

    // ── Importación ──────────────────────────────────────────────

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var chosen = _rows.Where(r => r.Selected).ToList();
        if (chosen.Count == 0)
        {
            MessageBox.Show("Marcá al menos una biblioteca.", "EcclesiaCast",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ImportButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        ProgressText.Visibility = Visibility.Visible;
        Progress.Visibility = Visibility.Visible;
        ProgressText.Text = "Leyendo las bibliotecas de EasyWorship…";
        Progress.IsIndeterminate = true;

        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

        var songs = new List<Song>();
        var unreadable = 0;
        foreach (var row in chosen)
        {
            try
            {
                var files = EasyWorshipDatabase.FindFiles(row.Folder)!.Value;
                songs.AddRange(EasyWorshipDatabase.ReadSongs(files.Songs, files.Words, out var bad));
                unreadable += bad;
            }
            catch (Exception ex) when (ex is InvalidDataException or Microsoft.Data.Sqlite.SqliteException or IOException)
            {
                Log.Error(ex, "No se pudo leer la biblioteca de EasyWorship {Path}", row.Folder);
                unreadable++;
            }
        }

        Progress.IsIndeterminate = false;
        Progress.Maximum = Math.Max(1, songs.Count);

        // A repeated title is asked about, with a box to answer the same for the rest.
        var session = _songSaver.Begin(several: true);

        int imported = 0, replaced = 0, discarded = 0, failed = 0;

        for (var i = 0; i < songs.Count; i++)
        {
            Progress.Value = i + 1;
            ProgressText.Text = $"Importando {i + 1} de {songs.Count}: {songs[i].Title}";

            // Let the window repaint between songs; this runs on the UI thread
            // because the repository isn't safe to hit from another one.
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

            try
            {
                switch (session.Save(songs[i]).Result)
                {
                    case SongSaveResult.Replaced: replaced++; break;
                    case SongSaveResult.Discarded: discarded++; break;
                    default: imported++; break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falló la importación de «{Title}» de EasyWorship", songs[i].Title);
                failed++;
            }
        }

        Imported = imported + replaced;

        var parts = new List<string> { $"{imported} canciones importadas de EasyWorship" };
        if (replaced > 0)
            parts.Add($"{replaced} reemplazadas");
        if (discarded > 0)
            parts.Add($"{discarded} descartadas por repetidas");
        if (unreadable + failed > 0)
            parts.Add($"{unreadable + failed} con error (ver el log)");
        Summary = string.Join(" · ", parts) + ".";

        Log.Information(
            "Importación de EasyWorship: {Imported} importadas, {Replaced} reemplazadas, {Discarded} descartadas, {Failed} con error",
            imported, replaced, discarded, unreadable + failed);

        // Nothing came through: say so out loud instead of leaving a line in
        // the status bar that is easy to miss and looks like success.
        if (imported + replaced == 0)
        {
            var reason = discarded > 0 && discarded == songs.Count
                ? "Todas ya estaban en tu biblioteca y las descartaste."
                : "No se pudo leer ninguna. ¿Está EasyWorship abierto? El detalle está en el registro de la aplicación.";

            MessageBox.Show(
                $"No se importó ninguna canción.\n\n{reason}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        DialogResult = true;
    }
}
