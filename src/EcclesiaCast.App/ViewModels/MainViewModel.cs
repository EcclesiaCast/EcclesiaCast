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
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
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
    private readonly IThemeRepository _themes;
    private readonly IThemeManagerDialog _themeManager;
    private readonly ISongDesigner _songDesigner;
    private readonly IQuickTextEditor _quickTextEditor;
    private readonly IMediaRepository _media;
    private readonly IMediaInspector _mediaInspector;

    /// <summary>Copied slide (label + text + style) for paste/duplicate.</summary>
    private (string Label, string Text, string? StyleJson)? _clipboardSlide;

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
    public ObservableCollection<ChapterOption> BibleChapters { get; } = [];

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

    /// <summary>Slide marked as "next up" by a reference search; -1 when none.</summary>
    [ObservableProperty]
    private int _previewSlideIndex = -1;

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
        IThemeRepository themes,
        IThemeManagerDialog themeManager,
        ISongDesigner songDesigner,
        IQuickTextEditor quickTextEditor,
        IMediaRepository media,
        IMediaInspector mediaInspector,
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
        _themes = themes;
        _themeManager = themeManager;
        _songDesigner = songDesigner;
        _quickTextEditor = quickTextEditor;
        _media = media;
        _mediaInspector = mediaInspector;
        Projection = projectionViewModel;

        _presentation.Changed += (_, _) => UpdateStateFlags();
        _projection.VisibilityChanged += (_, _) => IsProjecting = _projection.IsOutputVisible;
        Slides.CollectionChanged += (_, _) => HasSlides = Slides.Count > 0;
        UpdateStateFlags();
        RefreshDisplays();
        LoadSongs();
        LoadBibleVersions();
        LoadMedia();
    }

    // ── Medios (biblioteca con tabs por categoría) ───────────────

    /// <summary>All media, unfiltered; the bar shows the current tab's items.</summary>
    private readonly List<MediaItem> _allMedia = [];

    /// <summary>Items shown in the media bar (filtered by the current tab).</summary>
    public ObservableCollection<MediaItem> MediaItems { get; } = [];

    /// <summary>The tabs across the media bar (categories).</summary>
    public ObservableCollection<string> MediaTabs { get; } = [];

    [ObservableProperty]
    private string _selectedMediaTab = "Fondos";

    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };
    private static readonly HashSet<string> VideoExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".m4v", ".avi", ".mkv", ".wmv", ".webm" };

    private void LoadMedia()
    {
        _allMedia.Clear();
        _allMedia.AddRange(_media.GetAll());

        var keepTab = SelectedMediaTab;
        MediaTabs.Clear();
        foreach (var tab in _allMedia.Select(m => m.Category)
                     .Prepend("Fondos").Prepend("Anuncios")
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(t => t == "Fondos" ? 0 : t == "Anuncios" ? 1 : 2))
            MediaTabs.Add(tab);

        SelectedMediaTab = MediaTabs.Contains(keepTab) ? keepTab : MediaTabs.FirstOrDefault() ?? "Fondos";
        FilterMediaByTab();
    }

    partial void OnSelectedMediaTabChanged(string value) => FilterMediaByTab();

    private void FilterMediaByTab()
    {
        MediaItems.Clear();
        foreach (var item in _allMedia.Where(m =>
                     string.Equals(m.Category, SelectedMediaTab, StringComparison.OrdinalIgnoreCase)))
            MediaItems.Add(item);
    }

    [RelayCommand]
    private void NewMediaTab()
    {
        var name = _textPrompt.Ask("Nueva pestaña", "Nombre de la pestaña (ej. Jóvenes):");
        if (string.IsNullOrWhiteSpace(name))
            return;
        name = name.Trim();
        if (!MediaTabs.Contains(name, StringComparer.OrdinalIgnoreCase))
            MediaTabs.Add(name);
        SelectedMediaTab = name;
    }

    [RelayCommand]
    private void ImportMedia()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Agregar a «{SelectedMediaTab}»",
            Filter = "Imágenes y videos|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.wmv;*.webm"
                   + "|Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif"
                   + "|Videos|*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.wmv;*.webm",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true)
            return;

        var added = 0;
        foreach (var path in dialog.FileNames)
        {
            var ext = Path.GetExtension(path);
            MediaType? type = ImageExtensions.Contains(ext) ? MediaType.Image
                : VideoExtensions.Contains(ext) ? MediaType.Video
                : null;
            if (type is null)
                continue;

            var thumbnail = ShellThumbnail.Save(path) ?? (type == MediaType.Image ? path : null);

            _media.Add(new MediaItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                Type = type.Value,
                ThumbnailPath = thumbnail,
                Category = SelectedMediaTab,
                // Videos with audio default to a foreground announcement; silent
                // ones and images to a background — the operator can change it.
                Behavior = MediaBehavior.Background,
            });
            added++;
        }

        LoadMedia();
        StatusText = $"{added} medio(s) agregado(s) a «{SelectedMediaTab}».";
    }

    /// <summary>Click a media: background layers behind the text, foreground takes the screen.</summary>
    [RelayCommand]
    private void ApplyBackground(MediaItem? item)
    {
        if (item is null)
            return;

        _presentation.SetBackground(item);
        if (item.Behavior == MediaBehavior.Foreground)
            _presentation.ShowBackgroundOnly();

        if (SelectedDisplay is not null)
        {
            _projection.EnsureVisible(SelectedDisplay.Info);
            IsProjecting = true;
        }
        StatusText = item.Behavior == MediaBehavior.Foreground
            ? $"Primer plano: {item.Name} (a pantalla completa)."
            : $"Fondo: {item.Name}.";
    }

    [RelayCommand]
    private void ClearBackground()
    {
        _presentation.SetBackground(null);
        StatusText = "Fondo quitado.";
    }

    [RelayCommand]
    private void InspectMedia(MediaItem? item)
    {
        if (item is null)
            return;

        if (_mediaInspector.Edit(item, MediaTabs.ToList()))
        {
            _media.Update(item);
            LoadMedia();
            if (_presentation.Background?.Id == item.Id)
                _presentation.SetBackground(item); // re-aplica con las nuevas opciones
            StatusText = $"«{item.Name}» actualizado.";
        }
    }

    [RelayCommand]
    private void OpenMediaLocation(MediaItem? item)
    {
        if (item is null || !File.Exists(item.Path))
        {
            StatusText = "No se encuentra el archivo.";
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{item.Path}\"",
            UseShellExecute = true,
        });
    }

    [RelayCommand]
    private void DeleteMedia(MediaItem? item)
    {
        if (item is null)
            return;

        if (_presentation.Background?.Id == item.Id)
            _presentation.SetBackground(null);
        _media.Delete(item.Id);
        LoadMedia();
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

    // ── Temas ────────────────────────────────────────────────────

    private SlideTheme DefaultSongTheme =>
        Resolve(ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultSongThemeKey));

    private SlideTheme DefaultBibleTheme =>
        Resolve(ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultBibleThemeKey));

    private SlideTheme Resolve(int? themeId) =>
        (themeId is int id ? _themes.Get(id) : null) ?? SlideTheme.Fallback;

    private SlideTheme ResolveSongTheme(Song song) =>
        song.ThemeId is int id ? _themes.Get(id) ?? DefaultSongTheme : DefaultSongTheme;

    [RelayCommand]
    private void OpenThemes()
    {
        if (!_themeManager.Show())
            return;

        // Re-render whatever is on the grid with the fresh themes, keeping
        // the live slide projected.
        RebuildSlidesPreservingLive(() =>
        {
            if (_currentPassage is not null)
                LoadBiblePassage(_currentPassage);
            else if (SelectedSong is not null)
                BuildSongSlides();
        });
        StatusText = "Temas actualizados.";
    }

    /// <summary>Rebuilds the slide grid and re-projects the slide that was live.</summary>
    private void RebuildSlidesPreservingLive(Action rebuild)
    {
        var liveLabel = LiveSlideIndex >= 0 && LiveSlideIndex < Slides.Count
            ? Slides[LiveSlideIndex].Label
            : null;

        rebuild();

        if (liveLabel is not null)
        {
            var index = Slides.ToList().FindIndex(s => s.Label == liveLabel);
            if (index >= 0)
                GoLiveSlide(index);
        }
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
        PreviewSlideIndex = -1;
        _currentPassage = null;

        if (SelectedSong is null)
            return;

        var theme = ResolveSongTheme(SelectedSong);
        var caption = string.IsNullOrWhiteSpace(SelectedSong.Artist)
            ? SelectedSong.Title
            : $"{SelectedSong.Title} — {SelectedSong.Artist}";

        foreach (var section in SelectedSong.Sections)
        {
            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                section.Label,
                new SlideContent(section.Text, caption, Theme: theme, Override: section.GetOverride()),
                sectionId: section.Id));
        }

        PreviewSlide = Slides.FirstOrDefault()?.Slide;
    }

    private SongSection? SectionOf(SlideItemViewModel? item) =>
        item is null || SelectedSong is null || item.SectionId == 0
            ? null
            : SelectedSong.Sections.FirstOrDefault(s => s.Id == item.SectionId);

    /// <summary>Opens the full-song ProPresenter-style designer (right-click → Editar canción / Diseñar).</summary>
    [RelayCommand]
    private void EditSongDesign(SlideItemViewModel? item)
    {
        if (SelectedSong is null || (item is not null && item.SectionId == 0))
        {
            StatusText = "El diseño es para canciones; la Biblia usa su tema global (🎨 Temas).";
            return;
        }

        var index = item is not null
            ? SelectedSong.Sections.FindIndex(s => s.Id == item.SectionId)
            : 0;
        if (index < 0)
            index = 0;

        var theme = ResolveSongTheme(SelectedSong);
        if (!_songDesigner.Edit(SelectedSong, theme, index))
            return;

        var saved = _songs.Save(SelectedSong);
        RebuildSlidesPreservingLive(() =>
        {
            LoadSongs();
            SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        });
        StatusText = "Diseño de la canción guardado.";
    }

    /// <summary>Right-click → "Editar como texto plano": the lyrics editor.</summary>
    [RelayCommand]
    private void EditSongAsText(SlideItemViewModel? item)
    {
        if (SelectedSong is not null)
            EditSong();
    }

    /// <summary>Right-click on a slide → "Edición rápida": corrects just this slide's text.</summary>
    [RelayCommand]
    private void QuickEditSlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null)
            return;

        var edited = _quickTextEditor.Edit(section.Text);
        if (edited is null || edited == section.Text)
            return;

        if (edited.Length == 0)
        {
            StatusText = "El texto no puede quedar vacío.";
            return;
        }

        section.Text = edited;
        var saved = _songs.Save(SelectedSong!);
        RebuildSlidesPreservingLive(() =>
        {
            LoadSongs();
            SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        });
        StatusText = "Texto de la diapositiva corregido.";
    }

    // ── Copiar / pegar / duplicar / eliminar diapositivas ────────

    [RelayCommand]
    private void CopySlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null)
            return;
        _clipboardSlide = (section.Label, section.Text, section.StyleJson);
        StatusText = "Diapositiva copiada.";
    }

    [RelayCommand]
    private void PasteSlide(SlideItemViewModel? item)
    {
        if (_clipboardSlide is null || SelectedSong is null)
            return;

        var at = item is not null
            ? SelectedSong.Sections.FindIndex(s => s.Id == item.SectionId) + 1
            : SelectedSong.Sections.Count;
        if (at <= 0)
            at = SelectedSong.Sections.Count;

        var c = _clipboardSlide.Value;
        SelectedSong.Sections.Insert(at,
            new SongSection { Label = c.Label, Text = c.Text, StyleJson = c.StyleJson });
        ReindexAndSaveSong("Diapositiva pegada.");
    }

    [RelayCommand]
    private void DuplicateSlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null || SelectedSong is null)
            return;

        var at = SelectedSong.Sections.FindIndex(s => s.Id == section.Id) + 1;
        SelectedSong.Sections.Insert(at,
            new SongSection { Label = section.Label, Text = section.Text, StyleJson = section.StyleJson });
        ReindexAndSaveSong("Diapositiva duplicada.");
    }

    [RelayCommand]
    private void DeleteSlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null || SelectedSong is null)
            return;

        if (SelectedSong.Sections.Count <= 1)
        {
            StatusText = "La canción debe tener al menos una diapositiva.";
            return;
        }

        SelectedSong.Sections.RemoveAll(s => s.Id == section.Id);
        ReindexAndSaveSong("Diapositiva eliminada.");
    }

    private void ReindexAndSaveSong(string status)
    {
        if (SelectedSong is null)
            return;

        for (var i = 0; i < SelectedSong.Sections.Count; i++)
            SelectedSong.Sections[i].Order = i;

        var saved = _songs.Save(SelectedSong);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = status;
    }

    // ── Menú contextual de la lista de canciones ─────────────────

    [RelayCommand]
    private void EditSongDesignFor(Song? song)
    {
        if (song is null)
            return;
        SelectedSong = Songs.FirstOrDefault(s => s.Id == song.Id) ?? song;
        EditSongDesign(null);
    }

    [RelayCommand]
    private void EditSongTextFor(Song? song)
    {
        if (song is null)
            return;
        SelectedSong = Songs.FirstOrDefault(s => s.Id == song.Id) ?? song;
        EditSong();
    }

    [RelayCommand]
    private void DuplicateSong(Song? song)
    {
        var source = song is null ? null : _songs.Get(song.Id);
        if (source is null)
            return;

        var copy = new Song
        {
            Title = $"{source.Title} (copia)",
            Artist = source.Artist,
            Copyright = source.Copyright,
            ThemeId = source.ThemeId,
            Sections = source.Sections
                .Select(s => new SongSection { Order = s.Order, Label = s.Label, Text = s.Text, StyleJson = s.StyleJson })
                .ToList(),
        };

        var saved = _songs.Save(copy);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = $"Canción duplicada: \"{saved.Title}\".";
    }

    [RelayCommand]
    private void DeleteSongFor(Song? song)
    {
        if (song is null)
            return;
        SelectedSong = Songs.FirstOrDefault(s => s.Id == song.Id) ?? song;
        DeleteSong();
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

        RefreshAfterVersionChange();
    }

    /// <summary>Clicking a version's name makes it the only active one.</summary>
    [RelayCommand]
    private void SelectSoloVersion(BibleVersionOption? option)
    {
        if (option is null)
            return;

        SelectedBibleVersionOption = option;

        _suppressVersionEvents = true;
        foreach (var other in BibleVersionOptions)
            other.IsSelected = other == option;
        _suppressVersionEvents = false;

        _checkedVersionIds.Clear();
        _checkedVersionIds.Add(option.Info.Id);

        RefreshAfterVersionChange();
    }

    /// <summary>
    /// Refreshes the grid and — if a verse is live — re-projects it right
    /// away, so version changes update the output in real time.
    /// </summary>
    private void RefreshAfterVersionChange()
    {
        RebuildSlidesPreservingLive(() =>
        {
            LoadBibleBooksAvailable();
            if (_currentPassage is not null)
                LoadBiblePassage(_currentPassage);
        });
    }

    private void LoadBibleBooksAvailable()
    {
        var keepNumber = SelectedBibleBook?.Number;
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

        if (keepNumber is int n)
            SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == n);

        UpdateBibleViewState();
    }

    partial void OnSelectedBibleBookChanged(BibleBookInfo? value)
    {
        BibleChapters.Clear();
        ChaptersTitle = value?.Name ?? string.Empty;

        if (value is not null && PrimaryVersion is not null)
        {
            foreach (var chapter in _bibles.GetChapterNumbers(PrimaryVersion.Id, value.Number))
                BibleChapters.Add(new ChapterOption(chapter));
        }

        UpdateBibleViewState();
    }

    [RelayCommand]
    private void BackToBooks() => SelectedBibleBook = null;

    [RelayCommand]
    private void SelectChapter(ChapterOption? option)
    {
        if (option is null || SelectedBibleBook is null)
            return;

        // Browsing and typing a reference are two paths to the same grid;
        // clear the search box so they don't fight visually.
        BibleQuery = string.Empty;
        LoadBiblePassage(new BibleReference(SelectedBibleBook.Number, option.Number, null, null));
    }

    private void MarkChapterSelected(int? chapter)
    {
        foreach (var option in BibleChapters)
            option.IsSelected = option.Number == chapter;
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

            // Park the left panel on the referenced book (chapter grid view).
            if (SelectedBibleBook?.Number != reference.BookNumber)
                SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == reference.BookNumber);

            // Always load the whole chapter; the referenced verse becomes
            // the "next up" selection, projected with Enter.
            LoadBiblePassage(new BibleReference(reference.BookNumber, reference.Chapter, null, null));

            if (reference.VerseStart is int verse)
            {
                var index = Slides.ToList().FindIndex(s => s.Label == $"{reference.Chapter}:{verse}");
                SetPreviewIndex(index);
                if (index >= 0)
                    BibleStatusText = $"Versículo {verse} seleccionado — Enter lo proyecta.";
            }
            else
            {
                SetPreviewIndex(Slides.FirstOrDefault(s => s.JumpTarget is null)?.Index ?? -1);
            }
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

        var theme = DefaultBibleTheme;

        // Opening card: jump back to the previous chapter (crossing into the
        // previous book's last chapter when needed). Never projected as-is.
        if (GetPreviousChapter(reference) is var (prevBook, prevChapter, prevName))
        {
            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                "ANTERIOR",
                new SlideContent($"◀  {prevName} {prevChapter}", "Volver al capítulo anterior", Theme: theme),
                new BibleReference(prevBook, prevChapter, null, null),
                jumpToEnd: true));
        }

        foreach (var v in verses)
        {
            var mainText = theme.ShowVerseNumbers ? $"{v.Verse}  {v.Text}" : v.Text;

            string? secondaryText = null;
            secondaryTexts?.TryGetValue((v.Chapter, v.Verse), out secondaryText);

            string caption;
            if (secondary is not null)
            {
                // Two versions: label each text inline with its abbreviation
                // (like BibleShow) and drop the version(s) from the reference.
                mainText = $"[{primary.Abbreviation}] {mainText}";
                if (secondaryText is not null)
                    secondaryText = $"[{secondary.Abbreviation}] {secondaryText}";
                caption = v.Reference;
            }
            else
            {
                caption = theme.ShowVersionName ? $"{v.Reference} · {primary.Abbreviation}" : v.Reference;
            }

            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                $"{v.Chapter}:{v.Verse}",
                new SlideContent(mainText, caption, secondaryText, theme)));
        }

        // Closing card: jump to the next chapter (crossing into the next
        // book when this was the last one). Never projected as-is.
        if (GetNextChapter(reference) is var (nextBook, nextChapter, nextName))
        {
            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                "SIGUIENTE",
                new SlideContent($"▶  {nextName} {nextChapter}", "Pasar al siguiente capítulo", Theme: theme),
                new BibleReference(nextBook, nextChapter, null, null)));
        }

        if (SelectedBibleBook?.Number == reference.BookNumber)
            MarkChapterSelected(reference.Chapter);

        PreviewSlide = Slides.FirstOrDefault(s => s.JumpTarget is null)?.Slide;
        BibleStatusText = $"{verses.Count} versículo(s). Clic en una diapositiva para proyectar.";
    }

    /// <summary>Previous chapter before a reference: within the book, or the previous book's last chapter.</summary>
    private (int Book, int Chapter, string Name)? GetPreviousChapter(BibleReference current)
    {
        var primary = PrimaryVersion;
        if (primary is null)
            return null;

        var previousInBook = _bibles.GetChapterNumbers(primary.Id, current.BookNumber)
            .Where(c => c < current.Chapter)
            .Cast<int?>()
            .LastOrDefault();
        if (previousInBook is int chapter)
            return (current.BookNumber, chapter, BibleBookCatalog.FindByNumber(current.BookNumber)?.Name ?? string.Empty);

        var previousBook = _bibles.GetAvailableBookNumbers(primary.Id)
            .Where(n => n < current.BookNumber)
            .Cast<int?>()
            .LastOrDefault();
        if (previousBook is int book)
        {
            var lastChapter = _bibles.GetChapterNumbers(primary.Id, book).Cast<int?>().LastOrDefault();
            if (lastChapter is int last)
                return (book, last, BibleBookCatalog.FindByNumber(book)?.Name ?? string.Empty);
        }

        return null;
    }

    /// <summary>Next chapter after a reference: within the book, or the next book's first chapter.</summary>
    private (int Book, int Chapter, string Name)? GetNextChapter(BibleReference current)
    {
        var primary = PrimaryVersion;
        if (primary is null)
            return null;

        var nextInBook = _bibles.GetChapterNumbers(primary.Id, current.BookNumber)
            .Where(c => c > current.Chapter)
            .Cast<int?>()
            .FirstOrDefault();
        if (nextInBook is int chapter)
            return (current.BookNumber, chapter, BibleBookCatalog.FindByNumber(current.BookNumber)?.Name ?? string.Empty);

        var nextBook = _bibles.GetAvailableBookNumbers(primary.Id)
            .Where(n => n > current.BookNumber)
            .Cast<int?>()
            .FirstOrDefault();
        if (nextBook is int book)
        {
            var firstChapter = _bibles.GetChapterNumbers(primary.Id, book).Cast<int?>().FirstOrDefault();
            if (firstChapter is int first)
                return (book, first, BibleBookCatalog.FindByNumber(book)?.Name ?? string.Empty);
        }

        return null;
    }

    /// <summary>Marks a slide as "next up": accent border, preview box, and scroll-into-view.</summary>
    private void SetPreviewIndex(int index)
    {
        foreach (var slide in Slides)
            slide.IsPreviewed = slide.Index == index;

        PreviewSlideIndex = index;
        if (index >= 0 && index < Slides.Count)
            PreviewSlide = Slides[index].Slide;
    }

    /// <summary>Projects the verse a typed reference points to (Enter in the search box).</summary>
    [RelayCommand]
    private void ProjectReference()
    {
        if (PreviewSlideIndex >= 0)
            GoLiveSlide(PreviewSlideIndex);
        else if (Slides.FirstOrDefault(s => s.JumpTarget is null) is { } first)
            GoLiveSlide(first.Index);
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

        // Chapter jump cards never project themselves: they load the target
        // passage and go live on its first verse (next) or its last verse
        // (previous, so backward reading is continuous) in one motion.
        if (item.JumpTarget is { } jump)
        {
            var jumpToEnd = item.JumpToEnd;

            if (SelectedBibleBook?.Number != jump.BookNumber)
                SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == jump.BookNumber);

            LoadBiblePassage(jump);

            var landing = jumpToEnd
                ? Slides.LastOrDefault(s => s.JumpTarget is null)
                : Slides.FirstOrDefault(s => s.JumpTarget is null);
            if (landing is not null)
                GoLiveSlide(landing.Index);
            return;
        }

        _presentation.GoLive(item.Slide);
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        LiveSlideIndex = index;
        PreviewSlideIndex = -1;
        foreach (var slide in Slides)
        {
            slide.IsLive = slide.Index == index;
            slide.IsPreviewed = false;
        }

        PreviewSlide = index + 1 < Slides.Count ? Slides[index + 1].Slide : null;
        Log.Debug("Slide {Index} en vivo: {Label}", index, item.Label);
        StatusText = $"En vivo: diapositiva {index + 1} de {Slides.Count}. Flechas ←→ para navegar · F1 Clear · F2 Black · F3 Logo.";
    }

    // ── Texto rápido ─────────────────────────────────────────────

    partial void OnQuickTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            PreviewSlide = new SlideContent(value.Trim(), Theme: DefaultSongTheme);
    }

    [RelayCommand]
    private void GoLive()
    {
        if (string.IsNullOrWhiteSpace(QuickText) || SelectedDisplay is null)
            return;

        _presentation.GoLive(new SlideContent(QuickText.Trim(), Theme: DefaultSongTheme));
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
