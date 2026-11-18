using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Data;
using Serilog;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>The programs songs can be brought over from.</summary>
public enum SongImportSource { ProPresenter, EasyWorship }

/// <summary>One song to bring over; <paramref name="Path"/> is its file, when it has one.</summary>
public sealed record SongImportItem(string Label, string? Path, Func<Song> Load);

/// <summary>A library with its tick box, bound to the list.</summary>
public sealed partial class SongLibraryRow(
    string title, string detail, int songCount, int cloudOnlyCount, Func<List<SongImportItem>> items)
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

    internal Func<List<SongImportItem>> Items => items;

    [ObservableProperty]
    private bool _selected = songCount > 0;
}

/// <summary>
/// Backs the window that brings songs over from another program (ProPresenter
/// or EasyWorship) without asking the operator to hunt for files: the operator
/// picks the program, this looks where that one keeps its libraries, lists
/// what it found with the song count, and imports the ticked ones in one go.
/// The folders it looks in come from <see cref="IPlatformPaths"/>.
/// </summary>
public sealed partial class SongImportViewModel : ObservableObject
{
    private readonly SongSaver _songSaver;
    private readonly IPlatformPaths _paths;
    private readonly Dictionary<SongImportSource, List<SongLibraryRow>> _rows = new()
    {
        [SongImportSource.ProPresenter] = [],
        [SongImportSource.EasyWorship] = [],
    };
    private readonly Dictionary<SongImportSource, string> _roots = new()
    {
        [SongImportSource.ProPresenter] = string.Empty,
        [SongImportSource.EasyWorship] = string.Empty,
    };

    public SongImportViewModel(SongSaver songSaver, IPlatformPaths paths)
    {
        _songSaver = songSaver;
        _paths = paths;
    }

    /// <summary>The program chosen.</summary>
    public SongImportSource Source { get; private set; } = SongImportSource.ProPresenter;

    /// <summary>True once both programs were looked for: before that, choosing one does nothing.</summary>
    public bool Discovered { get; private set; }

    /// <summary>How many songs were actually added (0 if cancelled).</summary>
    public int Imported { get; private set; }

    /// <summary>Line for the operator's status bar.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>The libraries found for the program chosen.</summary>
    public List<SongLibraryRow> Rows => _rows[Source];

    public string SourceName => Source == SongImportSource.ProPresenter ? "ProPresenter" : "EasyWorship";

    [ObservableProperty]
    private string _headerText = "Buscando…";

    [ObservableProperty]
    private string _rootText = string.Empty;

    [ObservableProperty]
    private bool _canImport;

    [ObservableProperty]
    private string _browseTip = string.Empty;

    [ObservableProperty]
    private bool _cloudWarningVisible;

    [ObservableProperty]
    private string _cloudWarningText = string.Empty;

    /// <summary>True from the moment the import starts: nothing else can be changed.</summary>
    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _progressIndeterminate;

    [ObservableProperty]
    private double _progressMaximum = 1;

    [ObservableProperty]
    private double _progressValue;

    // ── Discovery ────────────────────────────────────────────────

    /// <summary>
    /// Looks for both programs, so each choice says how much it found. Returns
    /// the one to open first: the one that has songs, and with both,
    /// ProPresenter as always.
    /// </summary>
    public SongImportSource DiscoverAll()
    {
        DiscoverProPresenter();
        DiscoverEasyWorship();
        Discovered = true;

        return Total(SongImportSource.ProPresenter) == 0 && Total(SongImportSource.EasyWorship) > 0
            ? SongImportSource.EasyWorship
            : SongImportSource.ProPresenter;
    }

    private int Total(SongImportSource source) => _rows[source].Sum(r => r.SongCount);

    /// <summary>«ProPresenter  ·  12»: the name of the program with how many songs it has, if any.</summary>
    public string ChoiceLabel(SongImportSource source)
    {
        var name = source == SongImportSource.ProPresenter ? "ProPresenter" : "EasyWorship";
        return Total(source) > 0 ? $"{name}  ·  {Total(source)}" : name;
    }

    private void DiscoverProPresenter()
    {
        var roots = ProPresenterLocator.FindRoots(_paths.DocumentFolders);
        foreach (var root in roots)
        {
            foreach (var library in ProPresenterLocator.ReadLibraries(root))
                _rows[SongImportSource.ProPresenter].Add(ProPresenterRow(library));
        }

        _roots[SongImportSource.ProPresenter] = string.Join("\n", roots);
    }

    private void DiscoverEasyWorship()
    {
        var libraries = EasyWorshipDatabase.FindLibraries(_paths.SoftouchFolder);
        foreach (var folder in libraries)
            AddEasyWorship(folder);

        _roots[SongImportSource.EasyWorship] = string.Join("\n", libraries);
    }

    private static SongLibraryRow ProPresenterRow(ProPresenterLibrary library) => new(
        library.Name, library.Path, library.SongCount, library.CloudOnlyCount,
        () => library.SongFiles
            .Select(path => new SongImportItem(
                Path.GetFileNameWithoutExtension(path), path,
                () => ProPresenterImporter.FromFile(Path.GetFileName(path), File.ReadAllBytes(path))))
            .ToList());

    private void AddEasyWorship(string folder)
    {
        if (_rows[SongImportSource.EasyWorship].Any(r => string.Equals(r.Folder, folder, StringComparison.OrdinalIgnoreCase)))
            return;

        if (EasyWorshipDatabase.FindFiles(folder) is not { } files)
            return;

        // «Easyworship · Default»: the folder above the version one is the profile. The version
        // folder (v6.1) names the format of the data, not the program: EasyWorship 8 keeps its
        // songs in a folder called v6.1, so it is left out of the name.
        var profile = Directory.GetParent(folder)?.Parent?.Parent;
        var title = profile?.Parent is { } product ? $"{product.Name} · {profile.Name}" : Path.GetFileName(folder);

        _rows[SongImportSource.EasyWorship].Add(new SongLibraryRow(
            title, folder, EasyWorshipDatabase.CountSongs(files.Songs), 0,
            () =>
            {
                // The words are read here, when importing, not while listing.
                var songs = EasyWorshipDatabase.ReadSongs(files.Songs, files.Words, out var bad);
                var items = songs
                    .Select(song => new SongImportItem(song.Title, null, () => song))
                    .ToList();
                for (var i = 0; i < bad; i++)
                    items.Add(new SongImportItem("(canción ilegible)", null, () => throw new InvalidDataException("Fila ilegible de EasyWorship")));
                return items;
            }));
    }

    // ── What the window shows for the program chosen ─────────────

    /// <summary>The operator chose a program (ignored until both were looked for).</summary>
    public void SelectSource(SongImportSource source)
    {
        if (!Discovered)
            return;

        Source = source;
        ShowSource();
    }

    /// <summary>Fills the texts for the chosen program with what was found.</summary>
    private void ShowSource()
    {
        var total = Total(Source);
        if (Rows.Count == 0)
        {
            HeaderText = $"No encontré {SourceName} en esta computadora.";
            RootText = Source == SongImportSource.ProPresenter
                ? "Si tus canciones están en otra carpeta (un disco externo, una copia de otra máquina), "
                  + "buscala con «Elegir carpeta…». Sirve tanto la carpeta ProPresenter entera como una "
                  + "biblioteca suelta llena de archivos .pro."
                : "Si tus canciones están en otra carpeta (una copia, otra computadora), buscala con "
                  + "«Elegir carpeta…»: la que tiene los archivos Songs.db y SongWords.db "
                  + "(en EasyWorship es «Databases\\Data»).";
            CanImport = false;
        }
        else
        {
            HeaderText = total > 0
                ? $"Encontré {total} canción(es) en {Rows.Count} biblioteca(s) de {SourceName}. ¿Las importo todas?"
                : $"Encontré {SourceName}, pero sus bibliotecas están vacías.";
            RootText = _roots[Source];
            CanImport = total > 0;
        }

        BrowseTip = Source == SongImportSource.ProPresenter
            ? "Si tus canciones están en otro lado (un disco externo, una copia), elegí la carpeta a mano"
            : "Si tus canciones están en otro lado (una copia, otra computadora), elegí la carpeta que tiene Songs.db y SongWords.db";
        ShowCloudWarning();
    }

    /// <summary>The text of the dialog that asks for a folder.</summary>
    public string BrowseTitle => Source == SongImportSource.ProPresenter
        ? "Carpeta de ProPresenter (o una biblioteca suelta)"
        : "Carpeta de EasyWorship con Songs.db y SongWords.db (Databases\\Data)";

    /// <summary>
    /// Warns when the songs are OneDrive placeholders. Without this the import
    /// looks like it works — the files are listed, the progress bar runs — and
    /// every single read fails, which is exactly what happened the first time
    /// this was used for real. Only ProPresenter's are plain files in a folder.
    /// </summary>
    private void ShowCloudWarning()
    {
        var cloudOnly = Source == SongImportSource.ProPresenter ? Rows.Sum(r => r.CloudOnlyCount) : 0;
        if (cloudOnly == 0)
        {
            CloudWarningVisible = false;
            return;
        }

        CloudWarningVisible = true;
        CloudWarningText =
            $"{cloudOnly} de esas canciones están en OneDrive y todavía no bajaron a esta computadora. "
            + "Así como están no se pueden leer.\n\n"
            + "Abrí la carpeta en el Explorador, hacé clic derecho sobre ella y elegí "
            + "«Conservar siempre en este dispositivo». Esperá a que termine de bajar "
            + "(el ícono pasa de nube ☁ a tilde verde ✓) y volvé a intentar.";
    }

    // ── A folder chosen by hand ──────────────────────────────────

    /// <summary>
    /// The operator chose a folder to look in. True when it held something
    /// (the lists and the texts are then up to date); false after a notice
    /// when it did not.
    /// </summary>
    public bool Browse(string folder)
    {
        var added = Source == SongImportSource.ProPresenter ? BrowseProPresenter(folder) : BrowseEasyWorship(folder);
        if (!added)
            return false;

        _roots[Source] = folder;
        ShowSource();
        HeaderText = $"{Total(Source)} canción(es) en {Rows.Count} biblioteca(s). ¿Las importo todas?";
        return true;
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

    /// <summary>The first library that has undownloaded songs, or else the first one; null when it is not there to open.</summary>
    public string? FolderToOpen()
    {
        var folder = Rows.FirstOrDefault(r => r.CloudOnlyCount > 0)?.Folder ?? Rows.FirstOrDefault()?.Folder;
        return folder is null || !Directory.Exists(folder) ? null : folder;
    }

    public void SetAll(bool selected)
    {
        foreach (var row in Rows)
            row.Selected = selected && row.SongCount > 0;
    }

    // ── Import ───────────────────────────────────────────────────

    /// <summary>
    /// Imports the songs of the ticked libraries. <paramref name="repaint"/>
    /// lets the window draw between songs (it runs on the screen's thread,
    /// because the repository is not safe to hit from another one). False
    /// when no library was ticked (it said so); true when it finished, even
    /// if nothing came through (it said that too).
    /// </summary>
    public async Task<bool> ImportAsync(Func<Task> repaint)
    {
        var chosen = Rows.Where(r => r.Selected).ToList();
        if (chosen.Count == 0)
        {
            Notice.Show("Marcá al menos una biblioteca.");
            return false;
        }

        IsImporting = true;
        ProgressText = $"Leyendo las bibliotecas de {SourceName}…";
        ProgressIndeterminate = true;

        await repaint();

        var items = new List<SongImportItem>();
        var unreadableLibraries = 0;
        foreach (var row in chosen)
        {
            try
            {
                items.AddRange(row.Items());
            }
            catch (Exception ex) when (ex is InvalidDataException or Microsoft.Data.Sqlite.SqliteException or IOException)
            {
                Log.Error(ex, "No se pudo leer la biblioteca de {Source} {Path}", SourceName, row.Folder);
                unreadableLibraries++;
            }
        }

        ProgressIndeterminate = false;
        ProgressMaximum = Math.Max(1, items.Count);

        // Titles are matched in memory by the saver: asking the database once
        // per song turns a 300-song import into 300 round trips. A repeated
        // title is asked about, with a box to answer the same for the rest.
        var session = _songSaver.Begin(several: true);

        int imported = 0, replaced = 0, skipped = 0, discarded = 0, failed = unreadableLibraries, cloudOnly = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ProgressValue = i + 1;
            ProgressText = $"Importando {i + 1} de {items.Count}: {item.Label}";

            // Let the window repaint between songs.
            await repaint();

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

        return true;
    }
}
