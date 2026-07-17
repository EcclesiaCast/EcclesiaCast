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
    /// <summary>Versions with their "active for projection" checkbox (up to 2 checked).</summary>
    public ObservableCollection<BibleVersionOption> BibleVersionOptions { get; } = [];

    public ObservableCollection<BibleVerseResult> BibleSearchResults { get; } = [];

    /// <summary>Books present in the primary version, in Bible order.</summary>
    public ObservableCollection<BibleBookInfo> BibleBooksAvailable { get; } = [];

    /// <summary>Chapters available for the selected book — the numbered button grid.</summary>
    public ObservableCollection<int> BibleChapters { get; } = [];

    /// <summary>Ids of the checked versions, oldest first; index 0 is the primary.</summary>
    private readonly List<int> _checkedVersionIds = [];

    private bool _suppressVersionEvents;

    /// <summary>The passage currently shown in the grid, to re-render when versions change.</summary>
    private BibleReference? _currentPassage;

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

    /// <summary>The row highlighted in the versions list (target of rename/delete).</summary>
    [ObservableProperty]
    private BibleVersionOption? _selectedBibleVersionOption;

    [ObservableProperty]
    private BibleBookInfo? _selectedBibleBook;

    [ObservableProperty]
    private string _chaptersTitle = string.Empty;

    [ObservableProperty]
    private bool _showBooksList = true;

    [ObservableProperty]
    private bool _showChaptersView;

    [ObservableProperty]
    private bool _showBibleResults;

    [ObservableProperty]
    private string _highlightText = string.Empty;

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

    /// <summary>The version projected as main text (the first one checked).</summary>
    private BibleVersionInfo? PrimaryVersion =>
        _checkedVersionIds.Count > 0
            ? BibleVersionOptions.FirstOrDefault(o => o.Info.Id == _checkedVersionIds[0])?.Info
            : null;

    /// <summary>The second checked version, shown below the main text.</summary>
    private BibleVersionInfo? SecondaryVersion =>
        _checkedVersionIds.Count > 1
            ? BibleVersionOptions.FirstOrDefault(o => o.Info.Id == _checkedVersionIds[1])?.Info
            : null;

    private void OnVersionOptionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressVersionEvents
            || e.PropertyName != nameof(BibleVersionOption.IsSelected)
            || sender is not BibleVersionOption option)
            return;

        if (option.IsSelected)
        {
            _checkedVersionIds.Add(option.Info.Id);

            // Up to two versions at a time: checking a third releases the oldest.
            while (_checkedVersionIds.Count > 2)
            {
                var oldestId = _checkedVersionIds[0];
                _checkedVersionIds.RemoveAt(0);
                var oldest = BibleVersionOptions.FirstOrDefault(o => o.Info.Id == oldestId);
                if (oldest is not null)
                {
                    _suppressVersionEvents = true;
                    oldest.IsSelected = false;
                    _suppressVersionEvents = false;
                }
            }
        }
        else
        {
            _checkedVersionIds.Remove(option.Info.Id);
        }

        LoadBibleBooksAvailable();
        if (_currentPassage is not null)
            LoadBiblePassage(_currentPassage);
    }

    private void LoadBibleBooksAvailable()
    {
        BibleBooksAvailable.Clear();
        SelectedBibleBook = null;

        if (PrimaryVersion is null)
        {
            UpdateBibleViewState();
            return;
        }

        foreach (var number in _bibles.GetAvailableBookNumbers(PrimaryVersion.Id))
        {
            var info = BibleBookCatalog.FindByNumber(number);
            if (info is not null)
                BibleBooksAvailable.Add(info);
        }

        UpdateBibleViewState();
    }

    partial void OnSelectedBibleBookChanged(BibleBookInfo? value)
    {
        BibleChapters.Clear();
        ChaptersTitle = value?.Name ?? string.Empty;

        if (value is not null && PrimaryVersion is not null)
        {
            foreach (var chapter in _bibles.GetChapterNumbers(PrimaryVersion.Id, value.Number))
                BibleChapters.Add(chapter);
        }

        UpdateBibleViewState();
    }

    [RelayCommand]
    private void BackToBooks() => SelectedBibleBook = null;

    [RelayCommand]
    private void SelectChapter(int chapter)
    {
        if (SelectedBibleBook is null)
            return;

        // Browsing and typing a reference are two paths to the same grid;
        // clear the search box so they don't fight visually.
        BibleQuery = string.Empty;
        LoadBiblePassage(new BibleReference(SelectedBibleBook.Number, chapter, null, null));
    }

    /// <summary>Which of the three views (books / chapters / results) the Bible tab shows.</summary>
    private void UpdateBibleViewState()
    {
        ShowBibleResults = BibleSearchResults.Count > 0;
        ShowChaptersView = !ShowBibleResults && SelectedBibleBook is not null;
        ShowBooksList = !ShowBibleResults && !ShowChaptersView;
    }

    partial void OnBibleQueryChanged(string value) => RunBibleQuery();

    private void RunBibleQuery()
    {
        BibleSearchResults.Clear();

        if (PrimaryVersion is null)
        {
            BibleStatusText = "Importá una Biblia con 📥 y marcá su casilla.";
            UpdateBibleViewState();
            return;
        }

        if (string.IsNullOrWhiteSpace(BibleQuery))
        {
            BibleStatusText = "Referencia (\"Juan 3:16\", \"sal 23\") o palabra a buscar.";
            UpdateBibleViewState();
            return;
        }

        var reference = BibleReferenceParser.TryParse(BibleQuery);
        if (reference is not null)
        {
            UpdateBibleViewState();
            LoadBiblePassage(reference);
            return;
        }

        if (BibleQuery.Trim().Length >= 3)
        {
            foreach (var result in _bibles.SearchText(PrimaryVersion.Id, BibleQuery.Trim()))
                BibleSearchResults.Add(result);

            BibleStatusText = BibleSearchResults.Count == 0
                ? "Sin resultados."
                : $"{BibleSearchResults.Count} resultado(s). Doble clic proyecta.";
        }

        UpdateBibleViewState();
    }

    private void LoadBiblePassage(BibleReference reference)
    {
        var primary = PrimaryVersion;
        if (primary is null)
            return;

        var verses = _bibles.GetPassage(primary.Id, reference);
        if (verses.Count == 0)
        {
            BibleStatusText = "No se encontraron versículos para esa referencia.";
            return;
        }

        var secondary = SecondaryVersion;
        Dictionary<(int Chapter, int Verse), string>? secondaryTexts = null;
        if (secondary is not null)
        {
            secondaryTexts = _bibles.GetPassage(secondary.Id, reference)
                .ToDictionary(v => (v.Chapter, v.Verse), v => v.Text);
        }

        _currentPassage = reference;
        Slides.Clear();
        LiveSlideIndex = -1;

        foreach (var v in verses)
        {
            var caption = secondary is null
                ? $"{v.Reference} · {primary.Abbreviation}"
                : $"{v.Reference} · {primary.Abbreviation} / {secondary.Abbreviation}";

            string? secondaryText = null;
            secondaryTexts?.TryGetValue((v.Chapter, v.Verse), out secondaryText);

            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                $"{v.Chapter}:{v.Verse}",
                new SlideContent(v.Text, caption, secondaryText)));
        }

        PreviewSlide = Slides.FirstOrDefault()?.Slide;
        BibleStatusText = $"{verses.Count} versículo(s). Clic en una diapositiva para proyectar.";
    }

    // ── Resaltado en vivo ────────────────────────────────────────

    partial void OnHighlightTextChanged(string value) => _presentation.SetHighlight(value);

    [RelayCommand]
    private void ClearHighlight() => HighlightText = string.Empty;

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

    private void LoadBibleVersions()
    {
        _suppressVersionEvents = true;

        var keepSelectedId = SelectedBibleVersionOption?.Info.Id;
        foreach (var option in BibleVersionOptions)
            option.PropertyChanged -= OnVersionOptionChanged;
        BibleVersionOptions.Clear();

        foreach (var version in _bibles.GetVersions())
        {
            var option = new BibleVersionOption(version)
            {
                IsSelected = _checkedVersionIds.Contains(version.Id),
            };
            option.PropertyChanged += OnVersionOptionChanged;
            BibleVersionOptions.Add(option);
        }

        // Drop checked ids whose version no longer exists.
        _checkedVersionIds.RemoveAll(id => BibleVersionOptions.All(o => o.Info.Id != id));

        // Something must be projectable: default to the first version.
        if (_checkedVersionIds.Count == 0 && BibleVersionOptions.Count > 0)
        {
            BibleVersionOptions[0].IsSelected = true;
            _checkedVersionIds.Add(BibleVersionOptions[0].Info.Id);
        }

        SelectedBibleVersionOption =
            BibleVersionOptions.FirstOrDefault(o => o.Info.Id == keepSelectedId)
            ?? BibleVersionOptions.FirstOrDefault();

        _suppressVersionEvents = false;
        LoadBibleBooksAvailable();
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

        // Leave the freshly imported version active for projection.
        var imported = BibleVersionOptions.FirstOrDefault(o => o.Info.Id == saved.Id);
        if (imported is not null)
        {
            SelectedBibleVersionOption = imported;
            if (!imported.IsSelected)
                imported.IsSelected = true;
        }

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
        var target = SelectedBibleVersionOption?.Info;
        if (target is null)
            return;

        var newName = _textPrompt.Ask("Renombrar versión", "Nuevo nombre:", target.Name);
        if (string.IsNullOrWhiteSpace(newName))
            return;

        _bibles.RenameVersion(target.Id, newName.Trim());
        LoadBibleVersions();
        BibleStatusText = "Versión renombrada.";
    }

    [RelayCommand]
    private void DeleteBibleVersion()
    {
        var target = SelectedBibleVersionOption?.Info;
        if (target is null)
            return;

        var confirm = MessageBox.Show(
            $"¿Eliminar la versión \"{target.Name}\" y sus {target.VerseCount} versículos?",
            "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        _bibles.DeleteVersion(target.Id);
        SelectedBibleVersionOption = null;
        LoadBibleVersions();
        BibleStatusText = "Versión eliminada.";
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
