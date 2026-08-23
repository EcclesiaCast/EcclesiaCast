using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Brings songs over from ProPresenter without asking the operator to hunt
/// for files: it looks where ProPresenter keeps its libraries, lists what it
/// found with the song count, and imports the ticked ones in one go.
/// </summary>
public partial class ProPresenterImportWindow : Window
{
    /// <summary>A library with its tick box, bound to the list.</summary>
    public sealed partial class Row(ProPresenterLibrary library) : ObservableObject
    {
        public ProPresenterLibrary Library { get; } = library;

        public string Name => $"{Library.Name}  ·  {Library.SongCount} canción(es)";

        public string Detail => Library.Path;

        [ObservableProperty]
        private bool _selected = library.SongCount > 0;
    }

    private readonly ISongRepository _songs;
    private readonly List<Row> _rows = [];

    public ProPresenterImportWindow(ISongRepository songs)
    {
        InitializeComponent();
        _songs = songs;
        LibrariesList.ItemsSource = _rows;

        Loaded += (_, _) => Discover();
    }

    /// <summary>How many songs were actually added (0 if cancelled).</summary>
    public int Imported { get; private set; }

    /// <summary>Line for the operator's status bar.</summary>
    public string Summary { get; private set; } = string.Empty;

    // ── Descubrimiento ───────────────────────────────────────────

    private void Discover()
    {
        _rows.Clear();

        var roots = ProPresenterLocator.FindRoots(DocumentFolders());
        foreach (var root in roots)
        {
            foreach (var library in ProPresenterLocator.ReadLibraries(root))
                _rows.Add(new Row(library));
        }

        LibrariesList.Items.Refresh();

        var total = _rows.Sum(r => r.Library.SongCount);
        if (roots.Count == 0)
        {
            HeaderText.Text = "No encontré ProPresenter en esta computadora.";
            RootText.Text = "Si tus canciones están en otra carpeta (un disco externo, una copia de "
                          + "otra máquina), buscala con «Elegir carpeta…». Sirve tanto la carpeta "
                          + "ProPresenter entera como una biblioteca suelta llena de archivos .pro.";
            ImportButton.IsEnabled = false;
            return;
        }

        HeaderText.Text = total > 0
            ? $"Encontré {total} canción(es) en {_rows.Count} biblioteca(s) de ProPresenter. ¿Las importo todas?"
            : "Encontré ProPresenter, pero sus bibliotecas están vacías.";
        RootText.Text = string.Join("\n", roots);
        ImportButton.IsEnabled = total > 0;
    }

    /// <summary>
    /// Where to look: Documents and the OneDrive mirror of it, in both the
    /// English and Spanish spellings — with OneDrive on, the real ProPresenter
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
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive"),
                 })
        {
            if (string.IsNullOrWhiteSpace(oneDrive))
                continue;
            folders.Add(Path.Combine(oneDrive, "Documents"));
            folders.Add(Path.Combine(oneDrive, "Documentos"));
        }

        return folders;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Carpeta de ProPresenter (o una biblioteca suelta)",
        };
        if (dialog.ShowDialog() != true)
            return;

        var folder = dialog.FolderName;

        // Either the ProPresenter root (which holds Libraries) or one library.
        var libraries = ProPresenterLocator.ReadLibraries(folder);
        if (libraries.Count == 0)
        {
            var single = ProPresenterLocator.ReadFolder(folder);
            if (single.SongCount == 0)
            {
                MessageBox.Show(
                    "En esa carpeta no hay archivos .pro de ProPresenter.",
                    "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            libraries = [single];
        }

        foreach (var library in libraries)
        {
            if (_rows.Any(r => string.Equals(r.Library.Path, library.Path, StringComparison.OrdinalIgnoreCase)))
                continue;
            _rows.Add(new Row(library));
        }

        LibrariesList.Items.Refresh();
        var total = _rows.Sum(r => r.Library.SongCount);
        HeaderText.Text = $"{total} canción(es) en {_rows.Count} biblioteca(s). ¿Las importo todas?";
        RootText.Text = folder;
        ImportButton.IsEnabled = total > 0;
    }

    private void CheckAll_Click(object sender, RoutedEventArgs e) => SetAll(true);

    private void CheckNone_Click(object sender, RoutedEventArgs e) => SetAll(false);

    private void SetAll(bool selected)
    {
        foreach (var row in _rows)
            row.Selected = selected && row.Library.SongCount > 0;
    }

    // ── Importación ──────────────────────────────────────────────

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var files = _rows.Where(r => r.Selected).SelectMany(r => r.Library.SongFiles).ToList();
        if (files.Count == 0)
        {
            MessageBox.Show("Marcá al menos una biblioteca.", "EcclesiaCast",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ImportButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        Progress.Visibility = Visibility.Visible;
        ProgressText.Visibility = Visibility.Visible;
        Progress.Maximum = files.Count;

        var skipExisting = SkipExistingCheck.IsChecked == true;

        // Titles are matched in memory: asking the database once per song
        // turns a 300-song import into 300 round trips.
        var existing = new HashSet<string>(
            _songs.Search(string.Empty).Select(s => s.Title),
            StringComparer.OrdinalIgnoreCase);

        int imported = 0, skipped = 0, failed = 0;

        for (var i = 0; i < files.Count; i++)
        {
            var path = files[i];
            Progress.Value = i + 1;
            ProgressText.Text = $"Importando {i + 1} de {files.Count}: {Path.GetFileNameWithoutExtension(path)}";

            // Let the window repaint between songs; this runs on the UI thread
            // because the repository isn't safe to hit from another one.
            await System.Windows.Threading.Dispatcher.Yield(
                System.Windows.Threading.DispatcherPriority.Background);

            try
            {
                var song = ProPresenterImporter.FromFile(Path.GetFileName(path), File.ReadAllBytes(path));

                if (song.Sections.Count == 0)
                {
                    Log.Warning("ProPresenter sin texto: {Path}", path);
                    skipped++;
                    continue;
                }

                if (skipExisting && !existing.Add(song.Title))
                {
                    skipped++;
                    continue;
                }

                _songs.Save(song);
                imported++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falló la importación de {Path}", path);
                failed++;
            }
        }

        Imported = imported;

        var parts = new List<string> { $"{imported} canciones importadas de ProPresenter" };
        if (skipped > 0)
            parts.Add($"{skipped} omitidas (vacías o repetidas)");
        if (failed > 0)
            parts.Add($"{failed} con error (ver el log)");
        Summary = string.Join(" · ", parts) + ".";

        Log.Information("Importación de ProPresenter: {Imported} importadas, {Skipped} omitidas, {Failed} con error",
            imported, skipped, failed);

        DialogResult = true;
    }
}
