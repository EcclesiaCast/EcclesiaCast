using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>A display plus the label shown to the operator.</summary>
public sealed record DisplayOption(DisplayInfo Info, string Label);

public sealed partial class MainViewModel : ObservableObject
{
    private const string OutputDisplayKey = "output.display";

    private readonly IDisplayProvider _displayProvider;
    private readonly IProjectionWindowService _projection;
    private readonly ISettingsStore _settings;
    private readonly IPresentationService _presentation;
    private readonly ISongRepository _songs;
    private readonly ISongEditor _songEditor;
    private readonly IBibleRepository _bibles;
    private readonly IBibleImportDialog _bibleImportDialog;
    private readonly ITextPrompt _textPrompt;

    /// <summary>What the projector is showing; the Live box binds to it.</summary>
    public ProjectionViewModel Projection { get; }

    public ObservableCollection<DisplayOption> Displays { get; } = [];
    public ObservableCollection<Song> Songs { get; } = [];
    public ObservableCollection<BibleVersionInfo> BibleVersions { get; } = [];
    public ObservableCollection<BibleVerseResult> BibleSearchResults { get; } = [];

    /// <summary>Books present in the selected version, in Bible order — for the Libro dropdown.</summary>
    public ObservableCollection<BibleBookInfo> BibleBooksAvailable { get; } = [];

    /// <summary>Chapters available for the selected book — for the Capítulo dropdown.</summary>
    public ObservableCollection<int> BibleChapters { get; } = [];

    /// <summary>The center slide grid — filled from either a song or a Bible passage.</summary>
    public ObservableCollection<SlideItemViewModel> Slides { get; } = [];

    [ObservableProperty]
    private DisplayOption? _selectedDisplay;

    [ObservableProperty]
    private string _statusText =
        "Creá una canción con ➕, o escribí un texto rápido y presioná Ctrl+Enter.";

    [ObservableProperty]
    private bool _isProjecting;

    [ObservableProperty]
    private string _quickText = string.Empty;

    /// <summary>What is being prepared; the Preview box binds to it.</summary>
    [ObservableProperty]
    private SlideContent? _previewSlide;

    [ObservableProperty]
    private string _overlayText = string.Empty;

    [ObservableProperty]
    private bool _isOverlayActive;

    [ObservableProperty]
    private bool _isClearActive;

    [ObservableProperty]
    private bool _isBlackActive;

    [ObservableProperty]
    private bool _isLogoActive;

    [ObservableProperty]
    private bool _hasSlides;

    [ObservableProperty]
    private bool _isBibleTabActive;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private BibleVersionInfo? _selectedBibleVersion;

    [ObservableProperty]
    private BibleBookInfo? _selectedBibleBook;

    [ObservableProperty]
    private int? _selectedBibleChapter;

    [ObservableProperty]
    private string _bibleQuery = string.Empty;

    [ObservableProperty]
    private string _bibleStatusText = "Escribí una referencia (ej. \"Juan 3:16\", \"sal 23\") o una palabra.";

    [ObservableProperty]
    private int _liveSlideIndex = -1;

    public MainViewModel(
        IDisplayProvider displayProvider,
        IProjectionWindowService projection,
        ISettingsStore settings,
        IPresentationService presentation,
        ISongRepository songs,
        ISongEditor songEditor,
        IBibleRepository bibles,
        IBibleImportDialog bibleImportDialog,
        ITextPrompt textPrompt,
        ProjectionViewModel projectionViewModel)
    {
        _displayProvider = displayProvider;
        _projection = projection;
        _settings = settings;
        _presentation = presentation;
        _songs = songs;
        _songEditor = songEditor;
        _bibles = bibles;
        _bibleImportDialog = bibleImportDialog;
        _textPrompt = textPrompt;
        Projection = projectionViewModel;

        _presentation.Changed += (_, _) => UpdateStateFlags();
        _projection.VisibilityChanged += (_, _) => IsProjecting = _projection.IsOutputVisible;
        Slides.CollectionChanged += (_, _) => HasSlides = Slides.Count > 0;
        UpdateStateFlags();
        RefreshDisplays();
        LoadSongs();
        LoadBibleVersions();
    }

    // ── Pantallas ────────────────────────────────────────────────

    [RelayCommand]
    private void RefreshDisplays()
    {
        var saved = SelectedDisplay?.Info.DeviceName ?? _settings.Get(OutputDisplayKey);

        Displays.Clear();
        var all = _displayProvider.GetDisplays();
        for (var i = 0; i < all.Count; i++)
        {
            var d = all[i];
            var label = $"Pantalla {i + 1} · {d.Width}×{d.Height}{(d.IsPrimary ? " (principal)" : string.Empty)}";
            Displays.Add(new DisplayOption(d, label));
        }

        // Prefer the remembered display, then the first secondary one.
        SelectedDisplay =
            Displays.FirstOrDefault(o => o.Info.DeviceName == saved)
            ?? Displays.FirstOrDefault(o => !o.Info.IsPrimary)
            ?? Displays.FirstOrDefault();
    }

    // ── Pestañas de biblioteca ───────────────────────────────────

    [RelayCommand]
    private void ShowSongsTab() => IsBibleTabActive = false;

    [RelayCommand]
    private void ShowBibleTab() => IsBibleTabActive = true;

    // ── Canciones ────────────────────────────────────────────────

    partial void OnSearchTextChanged(string value) => LoadSongs();

    partial void OnSelectedSongChanged(Song? value) => BuildSongSlides();

    private void LoadSongs()
    {
        var keepId = SelectedSong?.Id;
        Songs.Clear();
        foreach (var song in _songs.Search(SearchText))
            Songs.Add(song);
        SelectedSong = Songs.FirstOrDefault(s => s.Id == keepId) ?? SelectedSong;
    }

    private void BuildSongSlides()
    {
        Slides.Clear();
        LiveSlideIndex = -1;

        if (SelectedSong is null)
            return;

        var caption = string.IsNullOrWhiteSpace(SelectedSong.Artist)
            ? SelectedSong.Title
            : $"{SelectedSong.Title} — {SelectedSong.Artist}";

        foreach (var section in SelectedSong.Sections)
        {
            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                section.Label,
                new SlideContent(section.Text, caption)));
        }

        PreviewSlide = Slides.FirstOrDefault()?.Slide;
    }

    [RelayCommand]
    private void NewSong()
    {
        var created = _songEditor.Edit(null);
        if (created is null)
            return;

        var saved = _songs.Save(created);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = $"Canción \"{saved.Title}\" guardada.";
    }

    [RelayCommand]
    private void EditSong()
    {
        if (SelectedSong is null)
            return;

        var edited = _songEditor.Edit(SelectedSong);
        if (edited is null)
            return;

        var saved = _songs.Save(edited);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = $"Canción \"{saved.Title}\" actualizada.";
    }

    [RelayCommand]
    private void DeleteSong()
    {
        if (SelectedSong is null)
            return;

        var confirm = MessageBox.Show(
            $"¿Eliminar \"{SelectedSong.Title}\" de la biblioteca?",
            "EcclesiaCast",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        _songs.Delete(SelectedSong.Id);
        SelectedSong = null;
        LoadSongs();
        StatusText = "Canción eliminada.";
    }

    [RelayCommand]
    private void ImportSongs()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar canciones",
            Filter = "Canciones (*.txt, *.pro)|*.txt;*.pro|Texto plano (*.txt)|*.txt|ProPresenter 7 (*.pro)|*.pro",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true)
            return;

        int imported = 0, skipped = 0, failed = 0;

        foreach (var path in dialog.FileNames)
        {
            try
            {
                var song = Path.GetExtension(path).ToLowerInvariant() == ".pro"
                    ? ProPresenterImporter.FromFile(Path.GetFileName(path), File.ReadAllBytes(path))
                    : TxtSongImporter.FromText(Path.GetFileName(path), ReadTextFile(path));

                if (song.Sections.Count == 0)
                {
                    Log.Warning("Importación sin texto: {Path}", path);
                    skipped++;
                    continue;
                }

                var duplicate = _songs.Search(song.Title).Any(s =>
                    string.Equals(s.Title, song.Title, StringComparison.OrdinalIgnoreCase));
                if (duplicate)
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

        LoadSongs();

        var parts = new List<string> { $"{imported} canciones importadas" };
        if (skipped > 0)
            parts.Add($"{skipped} omitidas (vacías o ya existentes)");
        if (failed > 0)
            parts.Add($"{failed} con error (ver log)");
        StatusText = string.Join(" · ", parts) + ".";
    }

    /// <summary>Reads a text file as UTF-8, falling back to Latin-1.</summary>
    private static string ReadTextFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    // ── Biblia ───────────────────────────────────────────────────

    partial void OnBibleQueryChanged(string value) => RunBibleQuery();

    partial void OnSelectedBibleVersionChanged(BibleVersionInfo? value)
    {
        LoadBibleBooksAvailable();
        if (!string.IsNullOrWhiteSpace(BibleQuery))
            RunBibleQuery();
    }

    private void LoadBibleBooksAvailable()
    {
        BibleBooksAvailable.Clear();

        if (SelectedBibleVersion is null)
        {
            SelectedBibleBook = null;
            return;
        }

        foreach (var number in _bibles.GetAvailableBookNumbers(SelectedBibleVersion.Id))
        {
            var info = BibleBookCatalog.FindByNumber(number);
            if (info is not null)
                BibleBooksAvailable.Add(info);
        }

        // Keep the same book selected across a version switch when possible.
        var keepNumber = SelectedBibleBook?.Number;
        SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == keepNumber)
            ?? BibleBooksAvailable.FirstOrDefault();
    }

    partial void OnSelectedBibleBookChanged(BibleBookInfo? value)
    {
        BibleChapters.Clear();
        SelectedBibleChapter = null;

        if (value is null || SelectedBibleVersion is null)
            return;

        foreach (var chapter in _bibles.GetChapterNumbers(SelectedBibleVersion.Id, value.Number))
            BibleChapters.Add(chapter);
    }

    partial void OnSelectedBibleChapterChanged(int? value)
    {
        if (value is null || SelectedBibleBook is null)
            return;

        // Browsing by book/chapter and typing a reference are two paths to
        // the same grid; clear the search box so they don't fight visually.
        BibleQuery = string.Empty;
        LoadBiblePassage(new BibleReference(SelectedBibleBook.Number, value.Value, null, null));
    }

    private void RunBibleQuery()
    {
        BibleSearchResults.Clear();

        if (SelectedBibleVersion is null)
        {
            BibleStatusText = "Importá o elegí una versión de la Biblia primero.";
            return;
        }

        if (string.IsNullOrWhiteSpace(BibleQuery))
        {
            BibleStatusText = "Escribí una referencia (ej. \"Juan 3:16\", \"sal 23\") o una palabra.";
            return;
        }

        var reference = BibleReferenceParser.TryParse(BibleQuery);
        if (reference is not null)
        {
            LoadBiblePassage(reference);
            return;
        }

        if (BibleQuery.Trim().Length < 3)
            return;

        foreach (var result in _bibles.SearchText(SelectedBibleVersion.Id, BibleQuery.Trim()))
            BibleSearchResults.Add(result);

        BibleStatusText = BibleSearchResults.Count == 0
            ? "Sin resultados."
            : $"{BibleSearchResults.Count} resultado(s). Doble clic proyecta.";
    }

    private void LoadBiblePassage(BibleReference reference)
    {
        if (SelectedBibleVersion is null)
            return;

        var verses = _bibles.GetPassage(SelectedBibleVersion.Id, reference);
        if (verses.Count == 0)
        {
            BibleStatusText = "No se encontraron versículos para esa referencia.";
            return;
        }

        Slides.Clear();
        LiveSlideIndex = -1;

        foreach (var v in verses)
        {
            var caption = $"{v.Reference} · {SelectedBibleVersion.Abbreviation}";
            Slides.Add(new SlideItemViewModel(Slides.Count, $"{v.Chapter}:{v.Verse}", new SlideContent(v.Text, caption)));
        }

        PreviewSlide = Slides.FirstOrDefault()?.Slide;
        BibleStatusText = $"{verses.Count} versículo(s). Clic en una diapositiva para proyectar.";
    }

    [RelayCommand]
    private void ProjectBibleResult(BibleVerseResult? result)
    {
        if (result is null)
            return;

        LoadBiblePassage(new BibleReference(result.BookNumber, result.Chapter, null, null));
        var index = Slides.ToList().FindIndex(s => s.Label == $"{result.Chapter}:{result.Verse}");
        if (index >= 0)
            GoLiveSlide(index);
    }

    [RelayCommand]
    private void ImportBible()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar Biblia",
            Filter = "Biblias (*.json, *.xml)|*.json;*.xml|JSON|*.json|Zefania XML|*.xml",
        };
        if (dialog.ShowDialog() != true)
            return;

        ParsedBible parsed;
        try
        {
            var content = ReadTextFile(dialog.FileName);
            parsed = Path.GetExtension(dialog.FileName).ToLowerInvariant() == ".xml"
                ? ZefaniaBibleImporter.Parse(content)
                : JsonBibleImporter.Parse(content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló el análisis de la Biblia {Path}", dialog.FileName);
            MessageBox.Show(
                $"No se pudo leer el archivo:\n\n{ex.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (parsed.VerseCount == 0)
        {
            MessageBox.Show(
                "El archivo no contiene versículos reconocibles.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var suggestion = Path.GetFileNameWithoutExtension(dialog.FileName);
        var metadata = _bibleImportDialog.Prompt(suggestion);
        if (metadata is null)
            return;

        var saved = _bibles.Import(metadata.Name, metadata.Abbreviation, metadata.Language, parsed);
        LoadBibleVersions();
        SelectedBibleVersion = BibleVersions.FirstOrDefault(v => v.Id == saved.Id);

        var message = $"\"{saved.Name}\" importada: {parsed.VerseCount} versículos.";
        if (parsed.MissingBookNumbers.Count > 0)
            message += $" Faltan {parsed.MissingBookNumbers.Count} de los 66 libros.";
        BibleStatusText = message;
        Log.Information(
            "Biblia importada: {Name} ({Abbreviation}), {Verses} versículos, {Missing} libros faltantes",
            saved.Name, saved.Abbreviation, parsed.VerseCount, parsed.MissingBookNumbers.Count);
    }

    [RelayCommand]
    private void RenameBibleVersion()
    {
        if (SelectedBibleVersion is null)
            return;

        var newName = _textPrompt.Ask("Renombrar versión", "Nuevo nombre:", SelectedBibleVersion.Name);
        if (string.IsNullOrWhiteSpace(newName))
            return;

        _bibles.RenameVersion(SelectedBibleVersion.Id, newName.Trim());
        var keepId = SelectedBibleVersion.Id;
        LoadBibleVersions();
        SelectedBibleVersion = BibleVersions.FirstOrDefault(v => v.Id == keepId);
        BibleStatusText = "Versión renombrada.";
    }

    [RelayCommand]
    private void DeleteBibleVersion()
    {
        if (SelectedBibleVersion is null)
            return;

        var confirm = MessageBox.Show(
            $"¿Eliminar la versión \"{SelectedBibleVersion.Name}\" y sus {SelectedBibleVersion.VerseCount} versículos?",
            "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        _bibles.DeleteVersion(SelectedBibleVersion.Id);
        SelectedBibleVersion = null;
        LoadBibleVersions();
        BibleStatusText = "Versión eliminada.";
    }

    private void LoadBibleVersions()
    {
        var keepId = SelectedBibleVersion?.Id;
        BibleVersions.Clear();
        foreach (var v in _bibles.GetVersions())
            BibleVersions.Add(v);
        SelectedBibleVersion = BibleVersions.FirstOrDefault(v => v.Id == keepId) ?? BibleVersions.FirstOrDefault();
    }

    // ── Proyección de slides ─────────────────────────────────────

    [RelayCommand]
    private void ProjectSlide(SlideItemViewModel item) => GoLiveSlide(item.Index);

    [RelayCommand]
    private void ProjectFirstSlide() => GoLiveSlide(0);

    [RelayCommand]
    private void NextSlide() => GoLiveSlide(LiveSlideIndex < 0 ? 0 : LiveSlideIndex + 1);

    [RelayCommand]
    private void PreviousSlide() => GoLiveSlide(LiveSlideIndex - 1);

    private void GoLiveSlide(int index)
    {
        if (SelectedDisplay is null)
        {
            Log.Warning("GoLiveSlide({Index}) rechazado: no hay pantalla de salida seleccionada", index);
            StatusText = "No hay pantalla de salida seleccionada.";
            return;
        }

        if (index < 0 || index >= Slides.Count)
        {
            Log.Debug("GoLiveSlide({Index}) fuera de rango (slides: {Count})", index, Slides.Count);
            return;
        }

        var item = Slides[index];
        _presentation.GoLive(item.Slide);
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        LiveSlideIndex = index;
        foreach (var slide in Slides)
            slide.IsLive = slide.Index == index;

        PreviewSlide = index + 1 < Slides.Count ? Slides[index + 1].Slide : null;
        Log.Debug("Slide {Index} en vivo: {Label}", index, item.Label);
        StatusText = $"En vivo: diapositiva {index + 1} de {Slides.Count}. Flechas ←→ para navegar · F1 Clear · F2 Black · F3 Logo.";
    }

    // ── Texto rápido ─────────────────────────────────────────────

    partial void OnQuickTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            PreviewSlide = new SlideContent(value.Trim());
    }

    [RelayCommand]
    private void GoLive()
    {
        if (string.IsNullOrWhiteSpace(QuickText) || SelectedDisplay is null)
            return;

        _presentation.GoLive(new SlideContent(QuickText.Trim()));
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        LiveSlideIndex = -1;
        foreach (var slide in Slides)
            slide.IsLive = false;

        StatusText = "Texto rápido en vivo. F1 Clear · F2 Black · F3 Logo · Esc apaga la salida.";
    }

    // ── Aviso al pie ─────────────────────────────────────────────

    [RelayCommand]
    private void ShowOverlay()
    {
        if (string.IsNullOrWhiteSpace(OverlayText) || SelectedDisplay is null)
            return;

        _presentation.ShowOverlay(OverlayText.Trim());
        _projection.EnsureVisible(SelectedDisplay.Info);
        IsProjecting = true;
        StatusText = "Aviso al pie visible sobre la salida.";
    }

    [RelayCommand]
    private void HideOverlay()
    {
        _presentation.HideOverlay();
        StatusText = "Aviso al pie retirado.";
    }

    // ── Estados de salida ────────────────────────────────────────

    /// <summary>Turns the output window on (if a display is selected).</summary>
    private bool EnsureOutputOn()
    {
        if (SelectedDisplay is null)
        {
            StatusText = "No hay pantalla de salida seleccionada.";
            return false;
        }

        _projection.EnsureVisible(SelectedDisplay.Info);
        IsProjecting = true;
        return true;
    }

    [RelayCommand]
    private void ToggleOutput()
    {
        if (IsProjecting)
        {
            _projection.HideOutput();
            IsProjecting = false;
            StatusText = "Salida apagada.";
        }
        else if (EnsureOutputOn())
        {
            StatusText = "Salida en vivo.";
        }
    }

    [RelayCommand]
    private void ToggleClear()
    {
        _presentation.ToggleClear();
        EnsureOutputOn();
    }

    [RelayCommand]
    private void ToggleBlack()
    {
        _presentation.ToggleBlack();
        EnsureOutputOn();
    }

    [RelayCommand]
    private void ToggleLogo()
    {
        _presentation.ToggleLogo();
        EnsureOutputOn();
    }

    [RelayCommand]
    private void HideOutput()
    {
        _projection.HideOutput();
        IsProjecting = false;
        StatusText = "Salida apagada.";
    }

    private void UpdateStateFlags()
    {
        IsClearActive = _presentation.State == OutputState.Clear;
        IsBlackActive = _presentation.State == OutputState.Black;
        IsLogoActive = _presentation.State == OutputState.Logo;
        IsOverlayActive = _presentation.OverlayMessage is not null;
    }
}
