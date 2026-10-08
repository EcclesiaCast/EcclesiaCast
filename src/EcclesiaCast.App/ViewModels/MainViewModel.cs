using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>A display plus the label shown to the operator.</summary>
public sealed record DisplayOption(DisplayInfo Info, string Label);

public sealed partial class MainViewModel : ObservableObject, IRemoteHost
{
    private const string OutputDisplayKey = "output.display";
    private const string StageNotesKey = "stage.notes.text";
    private const string CountdownMinutesKey = "countdown.minutes";
    private const string CountdownClockKey = "countdown.clock";
    private const string CountdownHeadingKey = "countdown.heading";
    private const string CountdownFinishedKey = "countdown.finished";

    /// <summary>Marks that video posters were rebuilt with the VLC fallback (see RefreshVideoThumbnailsAsync).</summary>
    private const string VideoThumbnailsRebuiltKey = "media.video-thumbnails.rebuilt";

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
    private readonly IPlaylistRepository _playlists;
    private readonly IYouTubeBrowser _youTube;
    private readonly ILogoRepository _logos;
    private readonly ILogoManagerDialog _logoManager;
    private readonly IProPresenterImportDialog _proPresenterImport;
    private readonly IWebSongSearchDialog _webSongSearch;
    private readonly IStageWindowService _stage;
    private readonly IBackupDialog _backup;
    private readonly ICountdownDialog _countdownDialog;
    private readonly ISmartPlaylistDialog _smartPlaylistDialog;
    private readonly IGlobalSearchDialog _globalSearch;

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
    [NotifyPropertyChangedFor(nameof(OutputAspectRatio))]
    private DisplayOption? _selectedDisplay;

    /// <summary>Width over height of the output screen: LIVE and PREVIEW are drawn at this proportion.</summary>
    public double OutputAspectRatio => SelectedDisplay is { Info: var info }
        ? AspectFit.RatioOf(info.Width, info.Height)
        : AspectFit.DefaultRatio;

    [ObservableProperty]
    private string _statusText =
        "Creá una canción con ➕, o escribí un texto rápido y presioná Ctrl+Enter.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGridLiveBadge), nameof(ShowOtherLive))]
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

    /// <summary>Notes the operator wrote for the platform.</summary>
    [ObservableProperty]
    private string _stageNotesText = string.Empty;

    /// <summary>True while those notes are up on the stage display.</summary>
    [ObservableProperty]
    private bool _isStageNotesActive;

    /// <summary>True while the pre-service countdown is on the output.</summary>
    [ObservableProperty]
    private bool _isCountdownRunning;

    [ObservableProperty]
    private bool _isClearActive;

    [ObservableProperty]
    private bool _isBlackActive;

    [ObservableProperty]
    private bool _isLogoActive;

    /// <summary>True while a media background is applied — gates "Sin fondo".</summary>
    [ObservableProperty]
    private bool _hasBackground;

    [ObservableProperty]
    private bool _hasSlides;

    /// <summary>
    /// What the slide grid holds: «Hay libertad — IBI», «Juan 8 · RVR1960»,
    /// or the name of a media waiting to be projected. Shown above the grid.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridLive), nameof(HasGridTitle), nameof(ShowGridLiveBadge), nameof(ShowOtherLive))]
    private string? _gridTitle;

    /// <summary>The name of what was last put on screen, by the same naming.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridLive), nameof(ShowGridLiveBadge), nameof(ShowOtherLive))]
    private string? _liveTitle;

    /// <summary>True when the grid shows the very thing that is on screen.</summary>
    public bool IsGridLive => LiveTitle is not null && LiveTitle == GridTitle;

    public bool HasGridTitle => !string.IsNullOrEmpty(GridTitle);

    /// <summary>The «EN VIVO» mark next to the grid's title.</summary>
    public bool ShowGridLiveBadge => IsProjecting && IsGridLive;

    /// <summary>
    /// «En vivo: …» on the right of the header, when the screen shows
    /// something other than the grid (the operator is looking ahead).
    /// </summary>
    public bool ShowOtherLive => IsProjecting && LiveTitle is not null && !IsGridLive;

    /// <summary>
    /// A media item of the playlist opened with one click: the centre shows
    /// it, and only the double click (or the button) puts it on screen.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviewMedia), nameof(PreviewMediaImage), nameof(HasPreviewMediaImage))]
    private MediaItem? _previewMedia;

    public bool HasPreviewMedia => PreviewMedia is not null;

    public bool HasPreviewMediaImage => PreviewMediaImage is not null;

    /// <summary>The picture for <see cref="PreviewMedia"/>: its poster, or the image itself.</summary>
    public string? PreviewMediaImage => PreviewMedia switch
    {
        null => null,
        { ThumbnailPath: { Length: > 0 } poster } when File.Exists(poster) => poster,
        { Type: MediaType.Image } image when File.Exists(image.Path) => image.Path,
        _ => null,
    };

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
    private string _bibleStatusText = BibleHint;

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
        IPlaylistRepository playlists,
        IYouTubeBrowser youTube,
        ILogoRepository logos,
        ILogoManagerDialog logoManager,
        IProPresenterImportDialog proPresenterImport,
        IWebSongSearchDialog webSongSearch,
        IStageWindowService stage,
        IStageDesigner stageDesigner,
        IBackupDialog backup,
        ICountdownDialog countdownDialog,
        ISmartPlaylistDialog smartPlaylistDialog,
        IGlobalSearchDialog globalSearch,
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
        _playlists = playlists;
        _youTube = youTube;
        _logos = logos;
        _logoManager = logoManager;
        _proPresenterImport = proPresenterImport;
        _webSongSearch = webSongSearch;
        _stage = stage;
        _stageDesigner = stageDesigner;
        _backup = backup;
        _countdownDialog = countdownDialog;
        _smartPlaylistDialog = smartPlaylistDialog;
        _globalSearch = globalSearch;
        Projection = projectionViewModel;

        _presentation.Changed += (_, _) => UpdateStateFlags();
        _playbackTimer.Tick += OnPlaybackTick;
        _projection.VisibilityChanged += (_, _) => IsProjecting = _projection.IsOutputVisible;
        _projection.VideoEnded += (_, _) => OnProjectedVideoEnded();
        _stage.VisibilityChanged += (_, _) => IsStageVisible = _stage.IsVisible;
        LoadStageLayouts();
        LoadLowPerformance();
        Slides.CollectionChanged += (_, _) => HasSlides = Slides.Count > 0;
        UpdateStateFlags();
        RefreshDisplays();
        LoadSongs();
        LoadBibleVersions();
        LoadMedia();
        LoadLogos(null);
        LoadPlaylists(null);
        StageNotesText = _settings.Get(StageNotesKey) ?? string.Empty;
        _previewShowsStage = _settings.Get(PreviewShowsStageKey) == "1";
        RestoreRemote();
        _ = RefreshVideoThumbnailsAsync();

        // Light copies of the heavy videos, made in the background; the
        // status bar says when one is being prepared or is ready.
        LightVideoCache.StatusChanged += message =>
            Application.Current?.Dispatcher.BeginInvoke(() => StatusText = message);
        RefreshLightCopies();
    }

    // ── Playlist del servicio ────────────────────────────────────

    public ObservableCollection<Playlist> Playlists { get; } = [];
    public ObservableCollection<PlaylistItem> PlaylistItems { get; } = [];

    [ObservableProperty]
    private Playlist? _selectedPlaylist;

    [ObservableProperty]
    private PlaylistItem? _selectedPlaylistItem;

    /// <summary>True while the chosen list is one that fills itself.</summary>
    [ObservableProperty]
    private bool _isSmartPlaylist;

    /// <summary>Index in <see cref="PlaylistItems"/> of the item currently driving the grid; -1 if none.</summary>
    private int _currentPlaylistIndex = -1;

    /// <summary>True while OpenPlaylistItem loads content, so trackers aren't reset.</summary>
    private bool _openingPlaylistItem;

    private void LoadPlaylists(int? keepId)
    {
        keepId ??= SelectedPlaylist?.Id;
        Playlists.Clear();
        foreach (var playlist in _playlists.GetAll())
            Playlists.Add(playlist);
        SelectedPlaylist = Playlists.FirstOrDefault(p => p.Id == keepId) ?? Playlists.FirstOrDefault();
    }

    partial void OnSelectedPlaylistChanged(Playlist? value)
    {
        PlaylistItems.Clear();
        IsSmartPlaylist = value?.IsSmart == true;

        if (value is null)
            return;

        if (value.IsSmart)
        {
            FillSmartPlaylist(value);
            return;
        }

        foreach (var item in value.Items.OrderBy(i => i.Order))
            PlaylistItems.Add(item);
    }

    /// <summary>
    /// Works out what belongs in a smart list, right now. Nothing is stored:
    /// the answer changes with the library, which is the point of it.
    /// </summary>
    private void FillSmartPlaylist(Playlist playlist)
    {
        var chosen = Core.Playlists.SmartPlaylist.Select(
            _songs.Search(), playlist.Rule, playlist.RuleValue, DateTime.Now);

        var order = 0;
        foreach (var song in chosen)
        {
            PlaylistItems.Add(new PlaylistItem
            {
                Order = order++,
                Type = PlaylistItemType.Song,
                SongId = song.Id,
                Caption = string.IsNullOrWhiteSpace(song.Artist) ? song.Title : $"{song.Title} · {song.Artist}",
            });
        }

        var described = Core.Playlists.SmartPlaylist.Describe(playlist.Rule, playlist.RuleValue);
        StatusText = chosen.Count == 0
            ? $"«{playlist.Name}»: ninguna canción cumple ahora ({described})."
            : $"«{playlist.Name}»: {chosen.Count} {described}.";
    }

    /// <summary>
    /// Writes what the program is about to do on the trail the watchdog reads,
    /// so a freeze in the log says what it was busy with.
    /// </summary>
    private static void NoteAction(string what) => ActionTrail.Shared.Note(what);

    private void SavePlaylist()
    {
        if (SelectedPlaylist is null)
            return;

        NoteAction($"Guardar la playlist «{SelectedPlaylist.Name}»");

        // A smart list's items are worked out, not kept. Writing them down
        // would freeze today's answer into the library and quietly turn the
        // list into a hand-made one.
        if (SelectedPlaylist.IsSmart)
        {
            _playlists.Save(SelectedPlaylist);
            StatusText = "Esta lista se arma sola: no se le agregan ni quitan cosas a mano.";
            return;
        }

        for (var i = 0; i < PlaylistItems.Count; i++)
            PlaylistItems[i].Order = i;
        SelectedPlaylist.Items = PlaylistItems.ToList();
        var saved = _playlists.Save(SelectedPlaylist);
        LoadPlaylists(saved.Id);
    }

    [RelayCommand]
    private void NewPlaylist()
    {
        var name = _textPrompt.Ask("Nueva playlist", "Nombre (ej. Domingo 20/7):");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var saved = _playlists.Save(new Playlist { Name = name.Trim() });
        LoadPlaylists(saved.Id);
        StatusText = $"Playlist \"{saved.Name}\" creada.";
    }

    /// <summary>
    /// Creates a list that fills itself, or edits the rule of the one chosen.
    /// Churches accumulate songs faster than anyone remembers them; this is
    /// how the ones nobody has sung in months come back up.
    /// </summary>
    [RelayCommand]
    private void EditSmartPlaylist()
    {
        var editing = SelectedPlaylist is { IsSmart: true } ? SelectedPlaylist : null;

        var choice = _smartPlaylistDialog.Show(
            editing?.Name ?? "Hace rato que no cantamos",
            editing?.Rule ?? PlaylistRule.NotSungLately,
            editing?.RuleValue);

        if (choice is null)
            return;

        var playlist = editing ?? new Playlist();
        playlist.Name = choice.Name;
        playlist.Rule = choice.Rule;
        playlist.RuleValue = choice.Value;

        var saved = _playlists.Save(playlist);
        LoadPlaylists(saved.Id);

        // Re-reading it from the library is what fills it in.
        if (SelectedPlaylist?.Id == saved.Id)
            OnSelectedPlaylistChanged(SelectedPlaylist);

        var described = Core.Playlists.SmartPlaylist.Describe(saved.Rule, saved.RuleValue);
        StatusText = $"Lista inteligente «{saved.Name}»: {described}.";
    }

    [RelayCommand]
    private void RenamePlaylist()
    {
        if (SelectedPlaylist is null)
            return;

        // A smart list is renamed where its rule lives, so both are edited in
        // the same place instead of two different ones.
        if (SelectedPlaylist.IsSmart)
        {
            EditSmartPlaylist();
            return;
        }

        var name = _textPrompt.Ask("Renombrar playlist", "Nuevo nombre:", SelectedPlaylist.Name);
        if (string.IsNullOrWhiteSpace(name))
            return;
        SelectedPlaylist.Name = name.Trim();
        SavePlaylist();
    }

    [RelayCommand]
    private void DuplicatePlaylist()
    {
        if (SelectedPlaylist is null)
            return;

        // A smart list keeps no items of its own, so copying its rule would
        // hand back an empty list. Duplicating one freezes what it is showing
        // right now into an ordinary list — which is the point: the list
        // suggests the forgotten songs, and Sunday is built from some of them.
        var source = SelectedPlaylist.IsSmart ? PlaylistItems.ToList() : SelectedPlaylist.Items;

        var copy = new Playlist
        {
            Name = $"{SelectedPlaylist.Name} (copia)",
            Items = source
                .Select(i => new PlaylistItem
                {
                    Order = i.Order, Type = i.Type, Caption = i.Caption,
                    SongId = i.SongId, BibleVersionId = i.BibleVersionId,
                    BookNumber = i.BookNumber, Chapter = i.Chapter,
                    VerseStart = i.VerseStart, VerseEnd = i.VerseEnd, MediaId = i.MediaId,
                })
                .ToList(),
        };
        var saved = _playlists.Save(copy);
        LoadPlaylists(saved.Id);
        StatusText = $"Playlist duplicada: \"{saved.Name}\".";
    }

    [RelayCommand]
    private void DeletePlaylist()
    {
        if (SelectedPlaylist is null)
            return;
        var confirm = MessageBox.Show(
            $"¿Eliminar la playlist \"{SelectedPlaylist.Name}\"?",
            "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;
        _playlists.Delete(SelectedPlaylist.Id);
        SelectedPlaylist = null;
        LoadPlaylists(null);
        StatusText = "Playlist eliminada.";
    }

    private bool EnsurePlaylist()
    {
        // Adding to a list that fills itself would be lost the moment it is
        // worked out again, so say so instead of pretending it worked.
        if (SelectedPlaylist is { IsSmart: true } smart)
        {
            StatusText = $"«{smart.Name}» se arma sola: elegí otra lista para agregar cosas a mano.";
            return false;
        }

        if (SelectedPlaylist is not null)
            return true;
        NewPlaylist();
        return SelectedPlaylist is not null;
    }

    [RelayCommand]
    private void AddSongToPlaylist(Song? song) => InsertSongInPlaylist(song, PlaylistItems.Count);

    /// <summary>A song dragged from the list lands where it was dropped.</summary>
    public void InsertSongInPlaylist(Song? song, int index)
    {
        if (song is null || !EnsurePlaylist())
            return;

        index = Math.Clamp(index, 0, PlaylistItems.Count);
        if (index <= _currentPlaylistIndex)
            _currentPlaylistIndex++;

        PlaylistItems.Insert(index, new PlaylistItem
        {
            Type = PlaylistItemType.Song,
            Caption = string.IsNullOrWhiteSpace(song.Artist) ? song.Title : $"{song.Title} — {song.Artist}",
            SongId = song.Id,
        });
        SavePlaylist();
        StatusText = $"\"{song.Title}\" agregada a la playlist.";
    }

    /// <summary>
    /// The ➕ of the Bible panel. What it adds, in order: a range typed in
    /// the search box («juan 8 5-10»), else the verse on screen (or marked
    /// as next), else a single verse typed, else the whole chapter.
    /// </summary>
    [RelayCommand]
    private void AddCurrentPassageToPlaylist()
    {
        var typed = BibleReferenceParser.TryParse(BibleQuery);
        if (typed is { VerseStart: int start, VerseEnd: int end } && end > start)
        {
            AddPassageToPlaylist(typed);
            return;
        }

        if (_currentPassage is null)
        {
            if (typed is not null)
                AddPassageToPlaylist(typed);
            else
                StatusText = "Abrí un capítulo o escribí una referencia (juan 8 5-10) primero.";
            return;
        }

        var shown = BibleSlideAt(LiveSlideIndex) ?? BibleSlideAt(PreviewSlideIndex);
        if (shown is not null)
            AddPassageToPlaylist(new BibleReference(_currentPassage.BookNumber, shown.Chapter, shown.FirstVerse, shown.LastVerse));
        else if (typed?.VerseStart is not null)
            AddPassageToPlaylist(typed);
        else
            AddPassageToPlaylist(_currentPassage with { VerseStart = null, VerseEnd = null });
    }

    /// <summary>Right click on a verse card: just that verse.</summary>
    [RelayCommand]
    private void AddVerseSlideToPlaylist(SlideItemViewModel? slide)
    {
        if (slide?.Verses is { } verses && _currentPassage is not null)
            AddPassageToPlaylist(new BibleReference(_currentPassage.BookNumber, verses.Chapter, verses.FirstVerse, verses.LastVerse));
    }

    /// <summary>Right click on a verse card: from the verse on screen (or marked) to this one.</summary>
    [RelayCommand]
    private void AddRangeToSlideToPlaylist(SlideItemViewModel? slide)
    {
        if (_currentPassage is not null && RangeFromLive(slide) is var (first, last))
            AddPassageToPlaylist(new BibleReference(_currentPassage.BookNumber, slide!.Verses!.Chapter, first, last));
    }

    /// <summary>
    /// The verses between the one on screen and <paramref name="slide"/>, in
    /// either direction; null when there is no verse on screen in this
    /// chapter or it is this same one. Feeds the "from there to here" item.
    /// </summary>
    public (int First, int Last)? RangeFromLive(SlideItemViewModel? slide)
    {
        var live = BibleSlideAt(LiveSlideIndex) ?? BibleSlideAt(PreviewSlideIndex);
        if (slide?.Verses is not { } here || live is null || live.Chapter != here.Chapter
            || live.FirstVerse == here.FirstVerse)
            return null;

        return (Math.Min(live.FirstVerse, here.FirstVerse), Math.Max(live.LastVerse, here.LastVerse));
    }

    /// <summary>The verses behind the card at an index, if it is a Bible card.</summary>
    private VerseGroup? BibleSlideAt(int index) =>
        index >= 0 && index < Slides.Count ? Slides[index].Verses : null;

    /// <summary>A passage's name as the playlist shows it: «Juan 8:5-10 (RVR1960)».</summary>
    private string PassageCaption(BibleReference reference)
    {
        var bookName = BibleBookCatalog.FindByNumber(reference.BookNumber)?.Name ?? $"Libro {reference.BookNumber}";
        var verses = reference.VerseStart is int vs
            ? reference.VerseEnd is int ve && ve != vs ? $":{vs}-{ve}" : $":{vs}"
            : string.Empty;
        var version = PrimaryVersion is { } primary ? $" ({primary.Abbreviation})" : string.Empty;
        return $"{bookName} {reference.Chapter}{verses}{version}";
    }

    private void AddPassageToPlaylist(BibleReference reference)
    {
        if (PrimaryVersion is null || !EnsurePlaylist())
            return;

        if (_bibles.GetPassage(PrimaryVersion.Id, reference).Count == 0)
        {
            StatusText = $"{PassageCaption(reference)} no existe en esta versión.";
            return;
        }

        var caption = PassageCaption(reference);
        PlaylistItems.Add(new PlaylistItem
        {
            Type = PlaylistItemType.BiblePassage,
            Caption = caption,
            BibleVersionId = PrimaryVersion.Id,
            BookNumber = reference.BookNumber,
            Chapter = reference.Chapter,
            VerseStart = reference.VerseStart,
            VerseEnd = reference.VerseEnd,
        });
        SavePlaylist();
        StatusText = $"«{caption}» agregado a la playlist.";
    }

    [RelayCommand]
    private void AddMediaToPlaylist(MediaItem? item)
    {
        if (item is null || !EnsurePlaylist())
            return;
        PlaylistItems.Add(new PlaylistItem
        {
            Type = PlaylistItemType.Media,
            Caption = item.Name,
            MediaId = item.Id,
        });
        SavePlaylist();
        StatusText = $"\"{item.Name}\" agregado a la playlist.";
    }

    [RelayCommand]
    private void RemovePlaylistItem(PlaylistItem? item)
    {
        // Nothing to remove from a list that works itself out.
        if (item is null || IsSmartPlaylist)
            return;

        // The arrows walk the playlist from the item in use: removing one
        // above it shifts it up, and removing that very one leaves none.
        var index = PlaylistItems.IndexOf(item);
        if (index >= 0 && index < _currentPlaylistIndex)
            _currentPlaylistIndex--;
        else if (index == _currentPlaylistIndex)
            _currentPlaylistIndex = -1;

        PlaylistItems.Remove(item);
        SavePlaylist();
        StatusText = $"«{item.Caption}» quitado de la playlist.";
    }

    [RelayCommand]
    private void MovePlaylistItemUp(PlaylistItem? item)
    {
        if (item is null || IsSmartPlaylist)
            return;
        var index = PlaylistItems.IndexOf(item);
        if (index <= 0)
            return;
        PlaylistItems.Move(index, index - 1);
        SavePlaylist();
        SelectedPlaylistItem = PlaylistItems.FirstOrDefault(i => i.Caption == item.Caption && i.Order == index - 1);
    }

    [RelayCommand]
    private void MovePlaylistItemDown(PlaylistItem? item)
    {
        if (item is null || IsSmartPlaylist)
            return;
        var index = PlaylistItems.IndexOf(item);
        if (index < 0 || index >= PlaylistItems.Count - 1)
            return;
        PlaylistItems.Move(index, index + 1);
        SavePlaylist();
    }

    /// <summary>
    /// One click: loads the item into the operator without projecting it. A
    /// media item is only shown in the centre, since applying it would put it
    /// on the screen.
    /// </summary>
    [RelayCommand]
    private void OpenPlaylistItem(PlaylistItem? item) => LoadPlaylistItem(item, projectMedia: false);

    /// <summary>
    /// Loads a playlist item. <paramref name="projectMedia"/> says whether a
    /// media item goes on screen (double click, arrows) or just to the centre.
    /// </summary>
    private void LoadPlaylistItem(PlaylistItem? item, bool projectMedia)
    {
        if (item is null)
            return;

        _openingPlaylistItem = true;
        try
        {
            OpenPlaylistItemCore(item, projectMedia);
        }
        finally
        {
            _openingPlaylistItem = false;
        }

        _currentPlaylistIndex = PlaylistItems.IndexOf(item);
        SelectedPlaylistItem = item;
    }

    private void OpenPlaylistItemCore(PlaylistItem item, bool projectMedia)
    {
        switch (item.Type)
        {
            case PlaylistItemType.Song:
                IsBibleTabActive = false;
                var song = Songs.FirstOrDefault(s => s.Id == item.SongId);

                // The song list may be filtered by whatever was searched last;
                // the playlist is what the operator is following now, so drop
                // the filter rather than report the song as missing.
                if (song is null && !string.IsNullOrEmpty(SearchText))
                {
                    SearchText = string.Empty;
                    song = Songs.FirstOrDefault(s => s.Id == item.SongId);
                }

                if (song is null)
                {
                    StatusText = $"La canción \"{item.Caption}\" ya no está en la biblioteca.";
                    return;
                }

                // Choosing the song that is already selected changes nothing,
                // so its slides have to be rebuilt by hand.
                if (ReferenceEquals(SelectedSong, song))
                    BuildSongSlides();
                else
                    SelectedSong = song;
                break;

            case PlaylistItemType.BiblePassage:
                IsBibleTabActive = true;
                var option = BibleVersionOptions.FirstOrDefault(o => o.Info.Id == item.BibleVersionId);
                if (option is not null && PrimaryVersion?.Id != option.Info.Id)
                    SelectSoloVersion(option);
                if (item.BookNumber is int book && item.Chapter is int chapter)
                {
                    SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == book);
                    LoadBiblePassage(new BibleReference(book, chapter, null, null));
                    if (item.VerseStart is int verse)
                    {
                        var index = FindVerseSlide(chapter, verse);
                        SetPreviewIndex(index);
                    }
                }
                break;

            case PlaylistItemType.Media:
                var media = _allMedia.FirstOrDefault(m => m.Id == item.MediaId);
                if (media is null)
                {
                    StatusText = $"El medio \"{item.Caption}\" ya no está en la biblioteca.";
                    return;
                }

                if (projectMedia)
                    ProjectMedia(media);
                else
                    ShowMediaInCentre(media);
                break;
        }
    }

    /// <summary>
    /// Puts a media of the playlist in the centre, the way a song shows its
    /// slides: the operator sees what comes before it is on the screen.
    /// </summary>
    private void ShowMediaInCentre(MediaItem media)
    {
        // Leaving the song selected would make clicking it again do nothing,
        // since the grid only rebuilds when the selection changes.
        SelectedSong = null;
        Slides.Clear();
        LiveSlideIndex = -1;
        PreviewSlideIndex = -1;
        _currentPassage = null;
        PreviewSlide = null;

        PreviewMedia = media;
        GridTitle = media.Name;
        StatusText = $"«{media.Name}» listo: doble clic en la playlist (o el botón Proyectar) lo pone en pantalla.";
    }

    /// <summary>Puts a media on screen and names it above the grid when it takes the whole screen.</summary>
    private void ProjectMedia(MediaItem media)
    {
        ApplyBackground(media);
        if (media.Behavior == MediaBehavior.Foreground)
            LiveTitle = media.Name;
    }

    /// <summary>The «Proyectar» button under a media shown in the centre.</summary>
    [RelayCommand]
    private void ProjectPreviewMedia()
    {
        if (PreviewMedia is { } media)
            ProjectMedia(media);
    }

    /// <summary>Double-click: load the item AND put it live.</summary>
    [RelayCommand]
    private void ProjectPlaylistItem(PlaylistItem? item)
    {
        if (item is null)
            return;

        LoadPlaylistItem(item, projectMedia: true);

        if (item.Type is PlaylistItemType.Song or PlaylistItemType.BiblePassage)
        {
            if (item.Type == PlaylistItemType.BiblePassage && PreviewSlideIndex >= 0)
                GoLiveSlide(PreviewSlideIndex);
            else if (Slides.FirstOrDefault(s => s.JumpTarget is null) is { } first)
                GoLiveSlide(first.Index);
        }
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

    /// <summary>
    /// When on, finishing a video in this tab starts the next one, looping
    /// round at the end — the tab behaves like a playlist. Kept per tab in the
    /// settings, since tabs are just the media's category.
    /// </summary>
    [ObservableProperty]
    private bool _continuousPlayback;

    private static string ContinuousKey(string tab) => $"media.tab.{tab.ToLowerInvariant()}.continuous";

    private bool IsContinuousTab(string? tab) =>
        !string.IsNullOrWhiteSpace(tab) && _settings.Get(ContinuousKey(tab)) == "1";

    partial void OnContinuousPlaybackChanged(bool value)
    {
        if (_loadingMediaTab)
            return;

        _settings.Set(ContinuousKey(SelectedMediaTab), value ? "1" : "0");
        StatusText = value
            ? $"«{SelectedMediaTab}»: los videos se reproducen uno atrás del otro."
            : $"«{SelectedMediaTab}»: reproducción continua desactivada.";
    }

    /// <summary>True while the tab's own setting is being read into the checkbox.</summary>
    private bool _loadingMediaTab;

    // ── Fondo al azar ────────────────────────────────────────────

    private const string RandomEnabledKey = "media.random.enabled";
    private const string RandomTabKey = "media.random.tab";

    /// <summary>Draws backgrounds without repeating; see <see cref="ShuffleBag"/>.</summary>
    private readonly ShuffleBag _backgroundBag = new();

    /// <summary>The song the current random background was drawn for; 0 when none.</summary>
    private int _randomBackgroundSongId;

    /// <summary>
    /// Id of the last background the draw put up; 0 when none. Anything else
    /// on screen was chosen by hand, and the draw leaves it alone.
    /// </summary>
    private int _randomBackgroundId;

    /// <summary>
    /// When on, every song that goes live gets a different background from the
    /// chosen tab. The tab is the one the box was ticked in, so no extra
    /// setting is needed to say where the backgrounds come from.
    /// </summary>
    [ObservableProperty]
    private bool _randomBackgroundPerSong;

    /// <summary>Tab the automatic draw takes from ("Fondos" unless changed).</summary>
    private string RandomTab => _settings.Get(RandomTabKey) ?? "Fondos";

    partial void OnRandomBackgroundPerSongChanged(bool value)
    {
        if (_loadingMediaTab)
            return;

        _settings.Set(RandomEnabledKey, value ? "1" : "0");
        if (value)
            _settings.Set(RandomTabKey, SelectedMediaTab);

        _backgroundBag.Reset();
        _randomBackgroundSongId = 0;

        StatusText = value
            ? $"Cada canción se lleva un fondo distinto de «{SelectedMediaTab}»."
            : "Fondo al azar por canción desactivado.";
    }

    /// <summary>
    /// Backgrounds worth drawing from a tab: the ones that sit behind the text.
    /// A foreground item would cover the lyrics, which is never what someone
    /// asking for a random background wants.
    /// </summary>
    private List<MediaItem> BackgroundPool(string tab) =>
        _allMedia
            .Where(m => string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase))
            .Where(m => m.Behavior == MediaBehavior.Background)
            .ToList();

    /// <summary>Applies a background drawn at random from the tab on screen.</summary>
    [RelayCommand]
    private void RandomBackground()
    {
        var pool = BackgroundPool(SelectedMediaTab);
        if (pool.Count == 0)
        {
            StatusText = $"«{SelectedMediaTab}» no tiene fondos para sortear.";
            return;
        }

        var id = _backgroundBag.Take(pool.Select(m => m.Id).ToList(), Random.Shared);
        if (pool.FirstOrDefault(m => m.Id == id) is not { } picked)
            return;

        ApplyBackground(picked);
        _randomBackgroundId = picked.Id;
        StatusText = $"Fondo al azar: {picked.Name}.";
    }

    /// <summary>
    /// Gives a song its own background when the feature is on. Called as a
    /// slide goes live, but it only draws once per song: moving back and forth
    /// through the verses must not keep changing the background.
    /// </summary>
    /// <summary>
    /// Notes that this song has been sung, which is what the "songs we have
    /// stopped singing" list reads. Only the first slide of a run counts:
    /// walking the verses of one song is one use, not eight.
    /// </summary>
    private void NoteSongProjected()
    {
        if (SelectedSong is not { } song || song.Id <= 0 || _lastNotedSongId == song.Id)
            return;

        _lastNotedSongId = song.Id;
        var now = DateTime.Now;
        song.LastProjectedAt = now;

        try
        {
            _songs.MarkProjected(song.Id, now);
        }
        catch (Exception ex)
        {
            // Bookkeeping must never take the projection down mid-service.
            Log.Warning(ex, "No se pudo anotar el uso de la canción {Song}", song.Title);
        }
    }

    private int _lastNotedSongId;

    private void ApplyRandomBackgroundForSong()
    {
        if (!RandomBackgroundPerSong || SelectedSong is not { } song)
            return;

        if (song.Id == _randomBackgroundSongId)
            return;

        // A background picked from the media bar stays until the operator
        // changes it: the draw only fills an empty screen, or replaces a
        // background that was itself drawn.
        if (_presentation.Background is { } current && current.Id != _randomBackgroundId)
            return;

        var pool = BackgroundPool(RandomTab);
        if (pool.Count == 0)
            return;

        // Mark it drawn even if nothing lands, so an empty tab doesn't retry
        // on every single slide.
        _randomBackgroundSongId = song.Id;

        var id = _backgroundBag.Take(pool.Select(m => m.Id).ToList(), Random.Shared);
        if (pool.FirstOrDefault(m => m.Id == id) is { } picked)
        {
            ApplyBackground(picked);
            _randomBackgroundId = picked.Id;
        }
    }

    /// <summary>The item after this one in its tab, wrapping around at the end.</summary>
    private MediaItem? NextInTab(MediaItem current)
    {
        var siblings = _allMedia
            .Where(m => string.Equals(m.Category, current.Category, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (siblings.Count <= 1)
            return null;

        var index = siblings.FindIndex(m => m.Id == current.Id);
        return index < 0 ? siblings[0] : siblings[(index + 1) % siblings.Count];
    }

    private void LoadMedia()
    {
        _allMedia.Clear();
        _allMedia.AddRange(_media.GetAll());

        // An item with no tab name belongs to no tab and would never be shown
        // anywhere: send it to the first one.
        foreach (var orphan in _allMedia.Where(m => string.IsNullOrWhiteSpace(m.Category)))
        {
            orphan.Category = "Fondos";
            _media.Update(orphan);
        }

        var keepTab = SelectedMediaTab;
        MediaTabs.Clear();
        foreach (var tab in _allMedia.Select(m => m.Category)
                     .Prepend("YouTube").Prepend("Anuncios").Prepend("Fondos")
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(t => t == "Fondos" ? 0 : t == "Anuncios" ? 1 : t == "YouTube" ? 2 : 3))
            MediaTabs.Add(tab);

        SelectedMediaTab = MediaTabs.Contains(keepTab) ? keepTab : MediaTabs.FirstOrDefault() ?? "Fondos";
        FilterMediaByTab();
        SyncMediaTabOptions();
    }

    partial void OnSelectedMediaTabChanged(string value)
    {
        FilterMediaByTab();
        SyncMediaTabOptions();
    }

    /// <summary>
    /// Reads the current tab's own switches into the checkboxes. Called from
    /// <see cref="LoadMedia"/> too: on startup the tab is assigned its existing
    /// value, so the property-changed hook never fires and the boxes would
    /// stay blank while the settings said otherwise.
    /// </summary>
    private void SyncMediaTabOptions()
    {
        _loadingMediaTab = true;
        ContinuousPlayback = IsContinuousTab(SelectedMediaTab);
        // The box is ticked only on the tab the draw actually takes from, so
        // walking the tabs shows at a glance where the backgrounds come from.
        RandomBackgroundPerSong = _settings.Get(RandomEnabledKey) == "1"
            && string.Equals(SelectedMediaTab, RandomTab, StringComparison.OrdinalIgnoreCase);
        _loadingMediaTab = false;
    }

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
            Filter = "Imágenes, videos y documentos"
                   + "|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.wmv;*.webm;*.pdf;*.pptx;*.ppt"
                   + "|Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif"
                   + "|Videos|*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.wmv;*.webm"
                   + "|PDF y PowerPoint|*.pdf;*.pptx;*.ppt",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true)
            return;

        ImportMediaFiles(dialog.FileNames);
    }

    /// <summary>
    /// Brings files into the tab that is open: the part of «Agregar» that
    /// starts from paths, so dropping files from Explorer onto the media bar
    /// does exactly the same. Whatever the library does not take is counted
    /// and said in the status bar instead of being skipped in silence.
    /// </summary>
    public void ImportMediaFiles(IEnumerable<string> paths)
    {
        var added = 0;
        var skipped = 0;
        var documents = new List<string>();

        foreach (var path in paths)
        {
            // A PDF or a deck becomes one image per page, but drawing them
            // takes a moment, so they are done after this loop rather than
            // freezing the window in the middle of an import.
            var kind = MediaFiles.Classify(path);
            if (kind == MediaFileKind.Document && File.Exists(path))
            {
                documents.Add(path);
                continue;
            }

            MediaType? type = kind switch
            {
                MediaFileKind.Image => MediaType.Image,
                MediaFileKind.Video => MediaType.Video,
                _ => null,
            };
            if (type is null || !File.Exists(path))
            {
                skipped++;
                continue;
            }

            // Videos the shell can't decode get their poster from VLC afterwards,
            // in the background, so a batch import doesn't freeze the window.
            _media.Add(new MediaItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                Type = type.Value,
                ThumbnailPath = MediaThumbnails.Create(path, type.Value, videoEngine: null),
                Category = SelectedMediaTab,
                // Videos with audio default to a foreground announcement; silent
                // ones and images to a background — the operator can change it.
                Behavior = MediaBehavior.Background,
            });
            added++;
        }

        LoadMedia();
        var ignored = skipped > 0
            ? $" {skipped} archivo(s) no se agregaron: sólo entran imágenes, videos, PDF y PowerPoint."
            : string.Empty;
        StatusText = documents.Count > 0
            ? $"{added} medio(s) agregado(s). Preparando {documents.Count} documento(s)…{ignored}"
            : added == 0 && skipped > 0
                ? $"No se agregó nada: sólo entran imágenes, videos, PDF y PowerPoint."
                : $"{added} medio(s) agregado(s) a «{SelectedMediaTab}».{ignored}";
        _ = RefreshVideoThumbnailsAsync();
        RefreshLightCopies();

        if (documents.Count > 0)
            _ = ImportDocumentsAsync(documents);
    }

    /// <summary>
    /// Brings in a PDF or a PowerPoint deck as one background per page, in
    /// order. The pages are ordinary images from then on: they go in the
    /// playlist, take a theme's text over them, and need nothing installed to
    /// show — which is the point of rendering them now rather than on Sunday.
    /// </summary>
    private async Task ImportDocumentsAsync(IReadOnlyList<string> paths)
    {
        var tab = SelectedMediaTab;
        var pagesAdded = 0;
        string? failure = null;

        foreach (var path in paths)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var result = await DocumentImporter.ImportAsync(path);

            if (result.Error is not null)
            {
                failure = result.Error;
                continue;
            }

            for (var page = 0; page < result.Pages.Count; page++)
            {
                _media.Add(new MediaItem
                {
                    // Numbered so the pages sort and read in order in the tab.
                    Name = $"{name} · {page + 1}",
                    Path = result.Pages[page],
                    Type = MediaType.Image,
                    ThumbnailPath = MediaThumbnails.Create(result.Pages[page], MediaType.Image, videoEngine: null),
                    Category = tab,
                    Behavior = MediaBehavior.Foreground,
                });
                pagesAdded++;
            }
        }

        LoadMedia();

        StatusText = failure is not null
            ? failure
            : $"{pagesAdded} página(s) agregada(s) a «{tab}». Cada una es un fondo: arrastralas a la playlist para pasarlas con las flechas.";
    }

    /// <summary>
    /// Fills in the video posters the Windows shell couldn't produce, decoding a
    /// frame with VLC instead. Runs off the UI thread because each file costs a
    /// couple hundred milliseconds.
    /// </summary>
    /// <remarks>
    /// Until this existed, those videos showed the generic player icon (the VLC
    /// cone). The first run after the fix therefore rebuilds every video poster
    /// once, since we can't tell an old icon from a real frame after the fact.
    /// </remarks>
    /// <summary>
    /// Hands every video the program can project — the library, which holds
    /// the YouTube downloads too, and the video logos — to the light-copy
    /// cache. What is already done is skipped, so calling it after any
    /// change to the library is cheap.
    /// </summary>
    private void RefreshLightCopies()
    {
        var videos = _media.GetAll()
            .Where(m => m.Type == MediaType.Video)
            .Select(m => m.Path)
            .Concat(_logos.GetAll()
                .Where(l => l.Kind == LogoKind.Video && !string.IsNullOrWhiteSpace(l.Path))
                .Select(l => l.Path!));

        // IsProjecting is a plain field read: the cache asks from its own thread.
        LightVideoCache.Refresh(videos, () => IsProjecting);
    }

    private async Task RefreshVideoThumbnailsAsync()
    {
        // Videos need the VLC engine to get a poster; images and document
        // pages do not, so a missing engine must not stop their posters.
        var engine = App.VideoEngine;

        var rebuildAll = _settings.Get(VideoThumbnailsRebuiltKey) is null;

        // Images are included: their poster is a PNG kept apart from the
        // picture, and when that file disappears the card in the media bar
        // stays blank after every restart (it was only ever rebuilt for
        // videos).
        var pending = _media.GetAll()
            .Where(m => m.Type switch
            {
                MediaType.Video => engine is not null && !string.IsNullOrWhiteSpace(m.Path)
                                   && (rebuildAll || PosterPolicy.NeedsPoster(m, File.Exists)),
                MediaType.Image => PosterPolicy.NeedsPoster(m, File.Exists),
                _ => false,
            })
            .ToList();

        if (pending.Count > 0)
        {
            var rebuilt = await Task.Run(() =>
            {
                var count = 0;
                foreach (var item in pending)
                {
                    if (rebuildAll && item.Type == MediaType.Video)
                        MediaThumbnails.Delete(item.ThumbnailPath);

                    var thumbnail = MediaThumbnails.Create(item.Path, item.Type, engine);
                    if (thumbnail is null)
                        continue;

                    item.ThumbnailPath = thumbnail;
                    _media.Update(item);
                    count++;
                }
                return count;
            });

            if (rebuilt > 0)
                LoadMedia();
        }

        // Only once the video engine could do the job; otherwise the next
        // run still has to rebuild the old generic icons.
        if (engine is not null)
            _settings.Set(VideoThumbnailsRebuiltKey, "1");

        // Sweep posters left behind by earlier rebuilds.
        var current = _media.GetAll().Select(m => m.ThumbnailPath).ToList();
        await Task.Run(() => MediaThumbnails.DeleteOrphans(current));
    }

    /// <summary>Click a media: background layers behind the text, foreground takes the screen.</summary>
    [RelayCommand]
    private void ApplyBackground(MediaItem? item)
    {
        if (item is null)
            return;

        // A tab set to play through can't have its videos looping, or they
        // would never reach the end that triggers the next one. The change is
        // made on a copy so the item's own setting stays as the operator left it.
        if (item.Type != MediaType.Image
            && item.EndBehavior == VideoEndBehavior.Loop
            && IsContinuousTab(item.Category))
        {
            item = item.Clone();
            item.EndBehavior = VideoEndBehavior.Stop;
        }

        NoteAction($"Poner el fondo «{item.Name}»");
        _presentation.SetBackground(item, ResolveFill(item));

        // Foreground = the media alone (no text). Background with no live slide
        // also shows alone; Black/Logo would hide it, so drop back to showing
        // the media immediately.
        if (item.Behavior == MediaBehavior.Foreground
            || _presentation.CurrentSlide is null
            || _presentation.State is OutputState.Black or OutputState.Logo)
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

    /// <summary>
    /// Takes the image/video off the output and leaves the text where it was.
    /// Applying a background parks the output on "background only", so without
    /// this the lyrics stayed hidden after the background was removed.
    /// </summary>
    [RelayCommand]
    private void ClearBackground()
    {
        NoteAction("Sacar el fondo");
        _presentation.SetBackground(null);

        if (_presentation.State == OutputState.Clear && _presentation.CurrentSlide is not null)
            _presentation.ToggleClear();

        StatusText = "Fondo quitado; la letra sigue en pantalla.";
    }

    /// <summary>A non-looping video finished: honour its end behaviour.</summary>
    private void OnProjectedVideoEnded()
    {
        var background = _presentation.Background;
        if (background is null || background.EndBehavior == VideoEndBehavior.Loop)
            return;

        // The tab plays through: hand over to the next item in it.
        if (IsContinuousTab(background.Category) && NextInTab(background) is { } next)
        {
            ApplyBackground(next);
            StatusText = $"Reproducción continua en «{background.Category}» → {next.Name}.";
            return;
        }

        if (background.EndBehavior == VideoEndBehavior.Logo)
        {
            _presentation.SetBackground(null);
            if (!IsLogoActive)
                _presentation.ToggleLogo();
            StatusText = $"\"{background.Name}\" terminó → logo.";
        }
        else
        {
            StatusText = $"\"{background.Name}\" terminó.";
        }
    }

    // ── Logos ────────────────────────────────────────────────────

    private const string ActiveLogoKey = "logo.active";

    /// <summary>The church's logos, in the order set in the manager.</summary>
    public ObservableCollection<Logo> Logos { get; } = [];

    [ObservableProperty]
    private Logo? _selectedLogo;

    /// <summary>Name shown next to the Logo button; falls back when none is set up.</summary>
    public string ActiveLogoName => SelectedLogo?.Name ?? "sin logo";

    /// <summary>True when the active logo is a file that can go behind the lyrics.</summary>
    public bool CanUseLogoAsBackground => SelectedLogo?.CanBeBackground == true;

    /// <summary>Marks that the starter logo was created, so deleting it sticks.</summary>
    private const string LogosSeededKey = "logo.seeded";

    private void LoadLogos(int? keepId)
    {
        keepId ??= SelectedLogo?.Id
            ?? (int.TryParse(_settings.Get(ActiveLogoKey), out var saved) ? saved : null);

        // First run: leave one text logo in place so F3 shows something, and
        // so the manager opens with an example instead of an empty list.
        if (_settings.Get(LogosSeededKey) is null)
        {
            _settings.Set(LogosSeededKey, "1");
            if (_logos.GetAll().Count == 0)
            {
                var starter = _logos.Save(new Logo
                {
                    Name = "Reunión general",
                    Kind = LogoKind.Text,
                    Text = "Bienvenidos",
                    Order = 0,
                });
                keepId ??= starter.Id;
            }
        }

        Logos.Clear();
        foreach (var logo in _logos.GetAll())
            Logos.Add(logo);

        SelectedLogo = Logos.FirstOrDefault(l => l.Id == keepId) ?? Logos.FirstOrDefault();
    }

    partial void OnSelectedLogoChanged(Logo? value)
    {
        _presentation.SetActiveLogo(value);
        OnPropertyChanged(nameof(ActiveLogoName));
        OnPropertyChanged(nameof(CanUseLogoAsBackground));

        if (value is not null)
            _settings.Set(ActiveLogoKey, value.Id.ToString());
    }

    /// <summary>Picks a logo from the button's dropdown and shows it right away.</summary>
    [RelayCommand]
    private void SelectLogo(Logo? logo)
    {
        if (logo is null)
            return;

        SelectedLogo = logo;

        // Already on the logo? Switching one for the other is the whole point.
        if (!IsLogoActive)
            ToggleLogo();
        else
            StatusText = $"Logo: {logo.Name}.";
    }

    // ── Búsqueda global ──────────────────────────────────────────

    /// <summary>
    /// One box over the whole library. Each panel had its own search, so
    /// finding a word meant first guessing whether it was a song, a verse or
    /// a video — and mid-service there is no time to guess.
    /// </summary>
    [RelayCommand]
    private void SearchEverything()
    {
        var hit = _globalSearch.Show(SearchEverywhere, SearchText);
        if (hit is null)
            return;

        switch (hit.Kind)
        {
            case SearchHitKind.Song when Songs.FirstOrDefault(s => s.Id == hit.Id) is { } song:
                IsBibleTabActive = false;
                SelectedSong = song;
                StatusText = $"«{song.Title}» cargada. Doble clic o Enter para proyectar.";
                break;

            case SearchHitKind.Verse:
                IsBibleTabActive = true;
                // The verse opens in the version it was found in, not in
                // whichever one happened to be selected.
                if (BibleVersionOptions.FirstOrDefault(o => o.Info.Id == hit.Id) is { } found)
                    SelectSoloVersion(found);
                BibleQuery = hit.Reference ?? hit.Title;
                StatusText = $"{hit.Title} cargado.";
                break;

            case SearchHitKind.Media when MediaItems.FirstOrDefault(m => m.Id == hit.Id) is { } media:
                SelectedMediaTab = media.Category;
                ApplyBackground(media);
                break;
        }
    }

    /// <summary>
    /// Looks through songs, every Bible version and the media library at
    /// once. Capped per kind so one enormous group cannot bury the others.
    /// </summary>
    private IReadOnlyList<SearchHit> SearchEverywhere(string query)
    {
        var hits = new List<SearchHit>();

        foreach (var song in _songs.Search(query).Take(12))
        {
            hits.Add(new SearchHit(
                SearchHitKind.Song, song.Id, "♪", song.Title,
                string.IsNullOrWhiteSpace(song.Artist) ? "Canción" : song.Artist));
        }

        // Every installed version is searched, and each result says which one
        // it came from: the same phrase reads differently in each translation.
        // The cap is per version so one of them cannot bury the others.
        var perVersion = Math.Clamp(24 / Math.Max(1, BibleVersionOptions.Count), 4, 12);
        foreach (var option in BibleVersionOptions)
        {
            foreach (var verse in _bibles.SearchText(option.Info.Id, query, perVersion))
            {
                hits.Add(new SearchHit(
                    SearchHitKind.Verse, option.Info.Id, "†",
                    $"{verse.Reference} · {option.Info.Abbreviation}", Shorten(verse.Text),
                    verse.BookNumber, verse.Chapter, verse.Verse, verse.Reference));
            }
        }

        foreach (var media in MediaItems.Where(m => m.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(8))
        {
            hits.Add(new SearchHit(
                SearchHitKind.Media, media.Id,
                media.Type == MediaType.Image ? "▣" : "▶", media.Name, media.Category));
        }

        return hits;
    }

    /// <summary>
    /// The screen that counts down to the start of the service. What the
    /// operator typed is remembered, because a church starts at the same time
    /// every Sunday and nobody should retype "Empezamos en" every week.
    /// </summary>
    [RelayCommand]
    private void OpenCountdown()
    {
        var saved = new CountdownSettings(
            int.TryParse(_settings.Get(CountdownMinutesKey), out var minutes) && minutes > 0
                ? minutes
                : CountdownSettings.Default.Minutes,
            _settings.Get(CountdownClockKey) ?? CountdownSettings.Default.Clock,
            _settings.Get(CountdownHeadingKey) ?? CountdownSettings.Default.Heading,
            _settings.Get(CountdownFinishedKey) ?? CountdownSettings.Default.FinishedMessage);

        var choice = _countdownDialog.Show(saved, _presentation.Countdown is not null);
        if (choice is null)
            return;

        if (choice.Stop)
        {
            _presentation.StopCountdown();
            StatusText = "Cuenta regresiva detenida.";
            return;
        }

        _settings.Set(CountdownMinutesKey, choice.Settings.Minutes.ToString());
        _settings.Set(CountdownClockKey, choice.Settings.Clock);
        _settings.Set(CountdownHeadingKey, choice.Settings.Heading);
        _settings.Set(CountdownFinishedKey, choice.Settings.FinishedMessage);

        if (choice.Countdown is not { } countdown)
            return;

        _presentation.StartCountdown(countdown);
        StatusText = $"Cuenta regresiva: {countdown.Format(DateTimeOffset.Now)}.";
    }

    /// <summary>
    /// The whole library is one file, so a backup is one file too. Volunteers
    /// were being told to go find it in AppData; now it is a menu item.
    /// </summary>
    [RelayCommand]
    private void SaveBackup() => _backup.SaveBackup();

    [RelayCommand]
    private void RestoreBackup() => _backup.RestoreBackup();

    [RelayCommand]
    private void OpenLogos()
    {
        if (!_logoManager.Show())
            return;

        LoadLogos(null);
        RefreshLightCopies();
        // Re-apply so the output picks up an edited logo without a toggle.
        _presentation.SetActiveLogo(SelectedLogo);
        StatusText = "Logos actualizados.";
    }

    /// <summary>
    /// Puts the active logo behind the lyrics as a background, blurred by the
    /// amount set for it — the usual "our logo, softened, under the words".
    /// </summary>
    [RelayCommand]
    private void UseLogoAsBackground()
    {
        if (SelectedLogo is not { CanBeBackground: true } logo)
        {
            StatusText = "Para usarlo de fondo, el logo tiene que ser una imagen o un video.";
            return;
        }

        _presentation.SetBackground(LogoBackground(logo));
        BackgroundBlur = logo.BackgroundBlur;

        // The logo is a background now, not the Logo state.
        if (IsLogoActive)
            _presentation.ToggleLogo();

        EnsureOutputOn();
        StatusText = $"«{logo.Name}» de fondo, con {logo.BackgroundBlur:0} % de desenfoque.";
    }

    /// <summary>
    /// Wraps a logo as a background media item. Its id is negative so the rest
    /// of the app can tell it apart from a real library item and never tries
    /// to write it back to the media table.
    /// </summary>
    private static MediaItem LogoBackground(Logo logo) => new()
    {
        Id = -1000 - logo.Id,
        Name = logo.Name,
        Path = logo.Path!,
        Type = logo.Kind == LogoKind.Video ? MediaType.Video : MediaType.Image,
        ThumbnailPath = logo.Kind == LogoKind.Video ? logo.PosterPath : logo.Path,
        Scaling = logo.Scaling,
        Behavior = MediaBehavior.Background,
        EndBehavior = VideoEndBehavior.Loop,
        Muted = true,
        Blur = logo.BackgroundBlur,
    };

    // ── Reproducción del fondo (barra de transporte) ─────────────

    /// <summary>Polls the output while a video plays; the transport bar binds to the result.</summary>
    private readonly System.Windows.Threading.DispatcherTimer _playbackTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300),
    };

    /// <summary>True while the timer writes the position, so it isn't read back as a seek.</summary>
    private bool _updatingPlayback;

    [ObservableProperty]
    private bool _hasVideoPlayback;

    [ObservableProperty]
    private bool _isVideoPlaying;

    [ObservableProperty]
    private double _playbackPositionSeconds;

    [ObservableProperty]
    private double _playbackDurationSeconds;

    [ObservableProperty]
    private string _playbackTimeText = "0:00 / 0:00";

    /// <summary>Blur over the background, 0–100; applies live and sticks to the media.</summary>
    [ObservableProperty]
    private double _backgroundBlur;

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        var state = _projection.Playback;

        // With the logo up the background video keeps running behind it, but
        // it isn't what the congregation sees, so its controls step aside.
        HasVideoPlayback = state.HasVideo && !IsLogoActive;
        IsVideoPlaying = state.IsPlaying;

        _updatingPlayback = true;
        PlaybackDurationSeconds = Math.Max(state.Duration.TotalSeconds, 0.001);
        PlaybackPositionSeconds = Math.Clamp(state.Position.TotalSeconds, 0, PlaybackDurationSeconds);
        _updatingPlayback = false;

        PlaybackTimeText = state.CanSeek
            ? $"{Clock(state.Position)} / {Clock(state.Duration)}"
            : state.HasVideo ? Clock(state.Position) : "0:00 / 0:00";
    }

    private static string Clock(TimeSpan value) =>
        value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");

    /// <summary>Starts polling when a video goes on the output (and stops when none is left).</summary>
    private void SyncPlaybackTimer()
    {
        var isVideo = _presentation.Background?.Type is MediaType.Video or MediaType.YouTube;
        if (isVideo && !_playbackTimer.IsEnabled)
            _playbackTimer.Start();
        else if (!isVideo && _playbackTimer.IsEnabled)
        {
            _playbackTimer.Stop();
            HasVideoPlayback = false;
            PlaybackTimeText = "0:00 / 0:00";
        }
    }

    partial void OnPlaybackPositionSecondsChanged(double value)
    {
        if (_updatingPlayback)
            return;
        _projection.SeekTo(TimeSpan.FromSeconds(value));
    }

    partial void OnBackgroundBlurChanged(double value)
    {
        var amount = Math.Clamp(value, 0, 100);
        Projection.BackgroundBlur = amount;

        // Remember it, so the same background comes back blurred. A negative id
        // means the background is really a logo, which owns its own setting.
        if (_presentation.Background is not { } background || Math.Abs(background.Blur - amount) <= 0.01)
            return;

        background.Blur = amount;

        if (background.Id > 0)
        {
            _media.Update(background);
        }
        else if (Logos.FirstOrDefault(l => -1000 - l.Id == background.Id) is { } logo)
        {
            logo.BackgroundBlur = amount;
            _logos.Save(logo);
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        // Pressing play with the output off would send sound to a dark screen.
        if (!IsVideoPlaying && !IsProjecting && !EnsureOutputOn())
            return;

        _projection.TogglePlayPause();
        OnPlaybackTick(null, EventArgs.Empty);
    }

    [RelayCommand]
    private void SkipBackward() => _projection.Skip(TimeSpan.FromSeconds(-10));

    [RelayCommand]
    private void SkipForward() => _projection.Skip(TimeSpan.FromSeconds(10));

    [RelayCommand]
    private void RestartVideo() => _projection.SeekTo(TimeSpan.Zero);

    [RelayCommand]
    private void ClearBlur() => BackgroundBlur = 0;

    // ── YouTube ──────────────────────────────────────────────────

    /// <summary>Opens the embedded browser: sign in with the church account and pick videos.</summary>
    [RelayCommand]
    private void OpenYouTube()
    {
        if (!WebViewProfile.IsRuntimeAvailable())
        {
            MessageBox.Show(
                "Falta el runtime de WebView2 (Microsoft Edge WebView2), que EcclesiaCast usa para YouTube.\n\n" +
                "Se descarga gratis desde el sitio de Microsoft; en Windows 11 suele venir instalado.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ids = _youTube.Browse();
        AddYouTubeVideos(ids, "desde el navegador");
    }

    /// <summary>Adds a video from a pasted link.</summary>
    [RelayCommand]
    private void AddYouTubeLink()
    {
        var text = _textPrompt.Ask("Agregar de YouTube", "Pegá el link del video:");
        if (string.IsNullOrWhiteSpace(text))
            return;

        var id = YouTubeUrl.TryParseVideoId(text);
        if (id is null)
        {
            StatusText = "Ese link no parece un video de YouTube.";
            return;
        }

        AddYouTubeVideos([id], "por link");
    }

    private void AddYouTubeVideos(IReadOnlyList<string> videoIds, string source)
    {
        var fresh = videoIds
            .Distinct()
            .Where(id => !_allMedia.Any(m => m.Type == MediaType.YouTube && m.YouTubeId == id))
            .ToList();
        if (fresh.Count == 0)
        {
            if (videoIds.Count > 0)
                StatusText = "Ese video ya estaba en la biblioteca.";
            return;
        }

        // Played over the internet the video is always YouTube's to serve (and
        // its ads, if the account has no Premium); a copy on this computer
        // depends on nothing. The operator picks, once for the whole batch.
        var choice = _textPrompt.Choose(
            "Agregar de YouTube",
            fresh.Count == 1
                ? "¿Cómo querés usar este video?"
                : $"¿Cómo querés usar estos {fresh.Count} videos?",
            [
                "🌐  Reproducir por internet",
                "💾  Descargar para usar sin internet",
            ]);
        if (choice < 0)
            return;

        // A download lands in the tab that was open, not in the YouTube one.
        var openTab = SelectedMediaTab;
        const string tab = "YouTube";

        foreach (var id in fresh)
        {
            _media.Add(new MediaItem
            {
                Name = $"YouTube {id}",
                Path = YouTubeUrl.WatchUrl(id),
                YouTubeId = id,
                Type = MediaType.YouTube,
                ThumbnailPath = YouTubeUrl.ThumbnailUrl(id),
                Category = tab,
                // Announcement videos play full screen with sound.
                Behavior = MediaBehavior.Foreground,
                EndBehavior = VideoEndBehavior.Logo,
                Muted = false,
            });
        }

        LoadMedia();
        SelectedMediaTab = tab;

        if (choice == 0)
        {
            StatusText = $"{fresh.Count} video(s) de YouTube agregado(s) {source}, para reproducir por internet. "
                       + "Renombralos en Propiedades.";
            return;
        }

        var downloaded = 0;
        foreach (var id in fresh)
        {
            if (_allMedia.FirstOrDefault(m => m.Type == MediaType.YouTube && m.YouTubeId == id) is not { } online)
                continue;
            if (DownloadYouTubeCore(online, openTab, removeOnlineEntry: true))
                downloaded++;
        }

        StatusText = downloaded == fresh.Count
            ? StatusText
            : $"Se bajaron {downloaded} de {fresh.Count}. Los que no, quedaron en «{tab}» para reproducir por internet.";
    }

    /// <summary>
    /// Saves a YouTube video as a local file, so the service doesn't depend on
    /// the connection holding up (and the quality stops being YouTube's call).
    /// The copy goes into the tab that is open; the YouTube entry is kept.
    /// </summary>
    [RelayCommand]
    private void DownloadYouTube(MediaItem? item)
    {
        if (item is not { Type: MediaType.YouTube } || string.IsNullOrWhiteSpace(item.YouTubeId))
        {
            StatusText = "Elegí un video de YouTube para descargar.";
            return;
        }

        DownloadYouTubeCore(item, SelectedMediaTab, removeOnlineEntry: false);
    }

    /// <summary>
    /// Runs the download window for <paramref name="item"/> and, when it works,
    /// adds the file to <paramref name="targetTab"/>. With
    /// <paramref name="removeOnlineEntry"/> the YouTube entry is dropped once the
    /// copy exists (it was only added as a stand-in); if the download fails it
    /// stays, so the video is never lost.
    /// </summary>
    private bool DownloadYouTubeCore(MediaItem item, string targetTab, bool removeOnlineEntry)
    {
        var window = new Views.YouTubeDownloadWindow(item.YouTubeId!, item.Name)
        {
            Owner = Application.Current.MainWindow,
        };

        if (window.ShowDialog() != true || window.DownloadedPath is not { } path)
        {
            StatusText = "La descarga no se completó.";
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(path);
        _media.Add(new MediaItem
        {
            Name = name,
            Path = path,
            Type = MediaType.Video,
            ThumbnailPath = MediaThumbnails.Create(path, MediaType.Video, App.VideoEngine),
            Category = targetTab,
            // A downloaded announcement behaves like the YouTube one did.
            Behavior = item.Behavior,
            EndBehavior = item.EndBehavior,
            Muted = item.Muted,
            Volume = item.Volume,
        });

        if (removeOnlineEntry)
            _media.Delete(item.Id);

        LoadMedia();
        SelectedMediaTab = targetTab;
        RefreshLightCopies();
        StatusText = $"«{name}» descargado y agregado a «{targetTab}».";
        return true;
    }

    /// <summary>
    /// The item chosen to fill the screen behind this one. Looked up here
    /// because the presentation layer knows nothing about the library.
    /// </summary>
    private MediaItem? ResolveFill(MediaItem? item) =>
        item?.FillMediaId is { } id ? _media.GetAll().FirstOrDefault(m => m.Id == id) : null;

    [RelayCommand]
    private void InspectMedia(MediaItem? item)
    {
        if (item is null)
            return;

        if (_mediaInspector.Edit(item, MediaTabs.ToList(), _media.GetAll()))
        {
            _media.Update(item);
            LoadMedia();
            if (_presentation.Background?.Id == item.Id)
                _presentation.SetBackground(item, ResolveFill(item)); // re-aplica con las nuevas opciones
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

        // A page of a PDF or a deck is a file this program made; taking the
        // item out of the library has to take the image with it.
        if (DocumentImporter.IsRenderedPage(item.Path))
            DocumentImporter.Forget(item.Path);

        LoadMedia();
    }

    // ── Control desde el celular ─────────────────────────────────

    private const string RemotePinKey = "remote.pin";
    private const string RemotePortKey = "remote.port";
    private const string RemoteEnabledKey = "remote.enabled";

    private RemoteControlServer? _remote;

    [ObservableProperty]
    private bool _isRemoteRunning;

    /// <summary>
    /// Starts listening. Left on between sessions on purpose: whoever set the
    /// phone up on Sunday shouldn't have to do it again next Sunday.
    /// </summary>
    private bool StartRemote()
    {
        _remote ??= new RemoteControlServer(this);
        if (_remote.IsRunning)
            return true;

        var pin = _settings.Get(RemotePinKey);
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4)
        {
            pin = Random.Shared.Next(1000, 10000).ToString();
            _settings.Set(RemotePinKey, pin);
        }

        var port = int.TryParse(_settings.Get(RemotePortKey), out var saved) ? saved : 8080;

        if (!_remote.Start(port, pin))
            return false;

        _settings.Set(RemotePortKey, _remote.Port.ToString());
        _settings.Set(RemoteEnabledKey, "1");
        IsRemoteRunning = true;
        return true;
    }

    /// <summary>Brings the remote back up when it was left on last time.</summary>
    private void RestoreRemote()
    {
        if (_settings.Get(RemoteEnabledKey) != "1")
            return;

        if (StartRemote())
            Log.Information("Control remoto restaurado en {Address}", _remote!.Address);
    }

    /// <summary>Turns the phone remote on and shows how to connect to it.</summary>
    [RelayCommand]
    private void OpenRemote()
    {
        if (!StartRemote())
        {
            MessageBox.Show(
                "No se pudo abrir ningún puerto para el control remoto. "
                + "Puede que otro programa los esté usando.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = new Views.RemoteControlWindow(_remote!)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();

        if (window.StopRequested)
        {
            _remote!.Stop();
            _settings.Set(RemoteEnabledKey, "0");
            IsRemoteRunning = false;
            StatusText = "Control desde el celular apagado.";
        }
        else
        {
            StatusText = $"Control desde el celular activo en {_remote!.Address}.";
        }
    }

    /// <summary>Shuts the server down when the app closes.</summary>
    public void StopRemote()
    {
        _remote?.Dispose();
        _remote = null;
        IsRemoteRunning = false;
    }

    RemoteState IRemoteHost.GetState()
    {
        var slides = Slides
            .Where(s => s.JumpTarget is null)
            .Select(s => new RemoteSlide(s.Index, s.Label, Shorten(s.Slide.MainText), s.IsLive))
            .ToList();

        var playlist = PlaylistItems
            .Select((item, index) => new RemotePlaylistItem(
                index,
                item.Caption,
                item.Type switch
                {
                    PlaylistItemType.BiblePassage => "📖",
                    PlaylistItemType.Media => "🎞",
                    _ => "🎵",
                }))
            .ToList();

        return new RemoteState(
            IsProjecting: IsProjecting,
            OutputState: _presentation.State.ToString(),
            SlideLabel: Projection.SlideLabel ?? "En vivo",
            LiveText: Projection.Slide?.MainText ?? string.Empty,
            NextText: Projection.NextSlide?.MainText ?? string.Empty,
            Status: StatusText,
            Slides: slides,
            Playlist: playlist,
            SongTitle: SelectedSong?.Title ?? (IsBibleTabActive ? "Biblia" : "EcclesiaCast"));
    }

    /// <summary>
    /// What the streaming page draws: the words as the congregation sees
    /// them, with the theme's styling, and nothing else. Clear, Black and
    /// Logo all mean "no words on screen" here — a clean feed follows the
    /// projector rather than second-guessing it.
    /// </summary>
    RemoteOutput IRemoteHost.GetOutput()
    {
        var slide = Projection.Slide;
        var theme = slide?.Theme ?? SlideTheme.Fallback;
        var over = slide?.Override;
        var countdown = _presentation.Countdown;

        return new RemoteOutput(
            ShowText: IsProjecting && _presentation.State == OutputState.Content && slide is not null,
            MainText: slide?.MainText ?? string.Empty,
            SecondaryText: slide?.SecondaryText ?? string.Empty,
            Caption: theme.ShowCaption ? slide?.Caption ?? string.Empty : string.Empty,
            Overlay: _presentation.OverlayMessage ?? string.Empty,
            Countdown: countdown?.Format(DateTimeOffset.Now) ?? string.Empty,
            CountdownHeading: countdown is null || countdown.HasFinished(DateTimeOffset.Now)
                ? string.Empty
                : countdown.Heading,
            FontFamily: over?.FontFamily ?? theme.FontFamily,
            FontSize: over?.FontSize ?? theme.MaxFontSize,
            Bold: over?.Bold ?? theme.Bold,
            Italic: over?.Italic ?? theme.Italic,
            Color: over?.TextColor ?? theme.TextColor,
            OutlineColor: over?.OutlineColor ?? theme.OutlineColor,
            OutlineWidth: over?.OutlineWidth ?? theme.OutlineWidth,
            AlignH: (over?.AlignH ?? theme.AlignH).ToString().ToLowerInvariant(),
            AlignV: (over?.AlignV ?? theme.AlignV).ToString().ToLowerInvariant());
    }

    void IRemoteHost.Execute(string action, int? index)
    {
        switch (action)
        {
            case "next": NextSlide(); break;
            case "prev": PreviousSlide(); break;
            case "clear": ToggleClear(); break;
            case "black": ToggleBlack(); break;
            case "logo": ToggleLogo(); break;
            case "output": ToggleOutput(); break;
            case "nobackground": ClearBackground(); break;
            case "slide" when index is int slide: GoLiveSlide(slide); break;
            case "playlist" when index is int item && item >= 0 && item < PlaylistItems.Count:
                ProjectPlaylistItem(PlaylistItems[item]);
                break;
            default:
                Log.Debug("El celular pidió una acción desconocida: {Action}", action);
                break;
        }
    }

    /// <summary>Slide previews travel on every poll, so they are kept short.</summary>
    private static string Shorten(string text)
    {
        var single = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return single.Length <= 70 ? single : single[..70] + "…";
    }

    // ── Modo PC de bajo rendimiento ──────────────────────────────

    /// <summary>
    /// Lightens the operator's panels and cuts the projector's transitions for
    /// computers without a graphics card. See <see cref="PerformanceMode"/>.
    /// </summary>
    [ObservableProperty]
    private bool _isLowPerformance;

    partial void OnIsLowPerformanceChanged(bool value)
    {
        _settings.Set(LowPerformance.SettingKey, value ? "1" : "0");
        PerformanceMode.Set(value);

        // The message is for a change the operator makes, not for starting up.
        if (!_startingUp)
            StatusText = value
                ? "Modo PC de bajo rendimiento: prendido."
                : "Modo PC de bajo rendimiento: apagado.";
    }

    private bool _startingUp = true;

    private void LoadLowPerformance()
    {
        // The installer asks once and leaves its answer in the registry; it
        // only counts until the operator has chosen something here.
        var initial = LowPerformance.InitialSetting(_settings.Get(LowPerformance.SettingKey), ReadInstallerChoice());
        if (initial is not null)
            _settings.Set(LowPerformance.SettingKey, initial);

        IsLowPerformance = LowPerformance.IsOn(_settings.Get(LowPerformance.SettingKey));
        _startingUp = false;
    }

    private static string? ReadInstallerChoice()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(LowPerformance.InstallerRegistryKey);
            return key?.GetValue(LowPerformance.InstallerValueName)?.ToString();
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Log.Debug(ex, "No se pudo leer la respuesta del instalador");
            return null;
        }
    }

    // ── Pantalla de escenario ────────────────────────────────────

    private const string StageDisplayKey = "stage.display";

    /// <summary>Displays offered for the stage screen, plus a "no usar" entry.</summary>
    public ObservableCollection<DisplayOption> StageDisplays { get; } = [];

    [ObservableProperty]
    private DisplayOption? _selectedStageDisplay;

    [ObservableProperty]
    private bool _isStageVisible;

    private readonly IStageDesigner _stageDesigner;

    private List<StageLayout> _stageLayouts = [];

    /// <summary>The saved arrangements of the stage screen, in the order they are offered.</summary>
    public IReadOnlyList<StageLayout> StageLayouts => _stageLayouts;

    /// <summary>
    /// The arrangement the stage screen (and its copy in the preview) is
    /// showing. The view redraws when this changes, so edits always replace
    /// the object instead of changing it in place.
    /// </summary>
    [ObservableProperty]
    private StageLayout _activeStageLayout = new();

    private void LoadStageLayouts()
    {
        var state = StageLayoutStore.Load(_settings);
        _stageLayouts = [.. state.Layouts];
        ActiveStageLayout = state.Active;
        OnPropertyChanged(nameof(StageLayouts));
    }

    /// <summary>Quick switch from the ▾ next to the stage button: takes effect on the open screen at once.</summary>
    [RelayCommand]
    private void SelectStageLayout(StageLayout? layout)
    {
        if (layout is null || ReferenceEquals(layout, ActiveStageLayout))
            return;

        ActiveStageLayout = layout;
        StageLayoutStore.Save(_settings, _stageLayouts, layout.Name);
        _stage.ApplyLayout(layout);
        StatusText = $"Escenario: {layout.Name}.";
    }

    /// <summary>Opens the editor; what it returns replaces the saved arrangements.</summary>
    [RelayCommand]
    private void EditStage()
    {
        var result = _stageDesigner.Edit(_stageLayouts, ActiveStageLayout.Name);
        if (result is null)
            return;

        _stageLayouts = [.. result.Layouts];
        StageLayoutStore.Save(_settings, _stageLayouts, result.ActiveName);
        OnPropertyChanged(nameof(StageLayouts));

        // A new object even when nothing about the active one changed: the
        // views compare by reference and must redraw after an edit.
        ActiveStageLayout = result.Active.Clone();
        _stageLayouts[_stageLayouts.FindIndex(l => l.Name == result.Active.Name)] = ActiveStageLayout;
        _stage.ApplyLayout(ActiveStageLayout);
        StatusText = "Escenarios guardados.";
    }

    private const string PreviewShowsStageKey = "layout.preview.stage";

    /// <summary>
    /// What the PREVIEW panel shows: the next slide (false) or the stage
    /// screen as the band sees it (true). Remembered between services.
    /// </summary>
    [ObservableProperty]
    private bool _previewShowsStage;

    partial void OnPreviewShowsStageChanged(bool value) =>
        _settings.Set(PreviewShowsStageKey, value ? "1" : "0");

    /// <summary>
    /// Turns the stage display on or off. It must not land on the same screen
    /// as the congregation's output — that would replace the projection with
    /// the musicians' view.
    /// </summary>
    [RelayCommand]
    private void ToggleStage()
    {
        if (IsStageVisible)
        {
            _stage.Hide();
            StatusText = "Pantalla de escenario apagada.";
            return;
        }

        if (SelectedStageDisplay is null)
        {
            StatusText = "Elegí en qué pantalla va el escenario.";
            return;
        }

        if (SelectedStageDisplay.Info.DeviceName == SelectedDisplay?.Info.DeviceName)
        {
            MessageBox.Show(
                "El escenario y la salida no pueden ir en la misma pantalla: la vista de los "
                + "músicos taparía lo que ve la congregación.\n\n"
                + "Elegí otro monitor para el escenario.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // With only two screens the stage can only land on the operator's own,
        // covering the panel it was started from. Say so, and say how to get
        // out, before it happens.
        if (IsOperatorDisplay(SelectedStageDisplay.Info))
        {
            var answer = MessageBox.Show(
                "El escenario va a ocupar esta misma pantalla y va a tapar el panel del operador.\n\n"
                + "Para cerrarlo, hacé doble clic sobre él.\n\n"
                + "¿Lo prendo igual?",
                "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes)
                return;
        }

        _stage.ShowOn(SelectedStageDisplay.Info, ActiveStageLayout);
        _settings.Set(StageDisplayKey, SelectedStageDisplay.Info.DeviceName);
        StatusText = $"Escenario en {SelectedStageDisplay.Label}.";
    }

    /// <summary>True when that display is the one holding the operator window.</summary>
    private static bool IsOperatorDisplay(DisplayInfo display)
    {
        var window = Application.Current?.MainWindow;
        if (window is null)
            return false;

        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return false;

        var screen = System.Windows.Forms.Screen.FromHandle(handle);
        return string.Equals(screen.DeviceName, display.DeviceName, StringComparison.Ordinal);
    }

    [RelayCommand]
    private void ResetStageTimer()
    {
        _stage.ResetTimer();
        StatusText = "Cronómetro del escenario en cero.";
    }

    // ── Pantallas ────────────────────────────────────────────────

    [RelayCommand]
    private void RefreshDisplays()
    {
        var saved = SelectedDisplay?.Info.DeviceName ?? _settings.Get(OutputDisplayKey);
        var savedStage = SelectedStageDisplay?.Info.DeviceName ?? _settings.Get(StageDisplayKey);

        Displays.Clear();
        StageDisplays.Clear();
        var all = _displayProvider.GetDisplays();
        for (var i = 0; i < all.Count; i++)
        {
            var d = all[i];
            var label = $"Pantalla {i + 1} · {d.Width}×{d.Height}{(d.IsPrimary ? " (principal)" : string.Empty)}";
            Displays.Add(new DisplayOption(d, label));
            StageDisplays.Add(new DisplayOption(d, label));
        }

        // Prefer the remembered display, then the first secondary one.
        SelectedDisplay =
            Displays.FirstOrDefault(o => o.Info.DeviceName == saved)
            ?? Displays.FirstOrDefault(o => !o.Info.IsPrimary)
            ?? Displays.FirstOrDefault();

        // The stage goes anywhere but the congregation's screen.
        SelectedStageDisplay =
            StageDisplays.FirstOrDefault(o => o.Info.DeviceName == savedStage)
            ?? StageDisplays.FirstOrDefault(o => o.Info.DeviceName != SelectedDisplay?.Info.DeviceName);
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
        var live = LiveSlideIndex >= 0 && LiveSlideIndex < Slides.Count
            ? Slides[LiveSlideIndex]
            : null;

        // Which part of a split verse was live, to land on the same one.
        var part = live?.Verses is { } before
            ? live.Index - FindVerseSlide(before.Chapter, before.FirstVerse)
            : 0;

        // The verse marked as next stays marked too, instead of falling back
        // to the chapter's first one.
        var previewed = BibleSlideAt(PreviewSlideIndex);

        rebuild();

        if (previewed is not null && FindVerseSlide(previewed.Chapter, previewed.FirstVerse) is var marked and >= 0)
            SetPreviewIndex(marked);

        if (live is null)
            return;

        // A verse is found by its number, not its label: after a theme change
        // it may take more or fewer slides, and its label changes with it.
        int index;
        if (live.Verses is { } verses)
        {
            index = FindVerseSlide(verses.Chapter, verses.FirstVerse);
            if (index >= 0 && part > 0 && index + part < Slides.Count
                && Slides[index + part].Verses?.FirstVerse == verses.FirstVerse)
                index += part;
        }
        else
        {
            index = Slides.ToList().FindIndex(s => s.Label == live.Label);
        }

        if (index >= 0)
            GoLiveSlide(index);
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
        NoteAction($"Abrir la canción «{SelectedSong?.Title}»");
        Slides.Clear();
        LiveSlideIndex = -1;
        PreviewSlideIndex = -1;
        _currentPassage = null;
        PreviewMedia = null;
        GridTitle = null;
        if (!_openingPlaylistItem)
            _currentPlaylistIndex = -1;

        if (SelectedSong is null)
            return;

        var theme = ResolveSongTheme(SelectedSong);
        var caption = string.IsNullOrWhiteSpace(SelectedSong.Artist)
            ? SelectedSong.Title
            : $"{SelectedSong.Title} — {SelectedSong.Artist}";
        GridTitle = caption;

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

        NoteAction($"Guardar la canción «{SelectedSong.Title}»");
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
        NoteAction($"Guardar la canción «{SelectedSong!.Title}»");
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

    /// <summary>
    /// Drag and drop in the slide grid: the dragged slide takes the place of
    /// the one it was dropped on, and the song keeps the new order. Only song
    /// slides move — a chapter's verses have an order of their own.
    /// </summary>
    /// <summary>
    /// Puts a dragged slide in the gap before the slide at <paramref name="gap"/>
    /// (0 = first, the number of slides = after the last), the way a word is
    /// dropped between two others: nothing else trades places.
    /// </summary>
    public void MoveSlideTo(SlideItemViewModel dragged, int gap)
    {
        var from = SectionOf(dragged);
        if (from is null || SelectedSong is null)
            return;

        var sections = SelectedSong.Sections;
        var fromIndex = sections.FindIndex(s => s.Id == from.Id);
        if (fromIndex < 0)
            return;

        // Taking it out shifts every gap after it one place to the left.
        var toIndex = Math.Clamp(gap > fromIndex ? gap - 1 : gap, 0, sections.Count - 1);
        if (toIndex == fromIndex)
            return;

        sections.RemoveAt(fromIndex);
        sections.Insert(toIndex, from);
        ReindexAndSaveSong($"«{from.Label}» pasó al lugar {toIndex + 1}.");
    }

    /// <summary>True for slides that can be dragged to a new place.</summary>
    public bool CanMoveSlide(SlideItemViewModel? item) => SectionOf(item) is not null;

    private void ReindexAndSaveSong(string status)
    {
        if (SelectedSong is null)
            return;

        for (var i = 0; i < SelectedSong.Sections.Count; i++)
            SelectedSong.Sections[i].Order = i;

        NoteAction($"Guardar la canción «{SelectedSong.Title}»");
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

        NoteAction($"Guardar la canción «{copy.Title}»");
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

        NoteAction($"Guardar la canción «{created.Title}»");
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

        NoteAction($"Guardar la canción «{edited.Title}»");
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

    /// <summary>Brings the whole ProPresenter library over in one step.</summary>
    [RelayCommand]
    private void ImportFromProPresenter()
    {
        if (_proPresenterImport.Show() is not { } summary)
            return;

        LoadSongs();
        StatusText = summary;
    }

    /// <summary>Finds songs on the internet (musica.com, LRCLIB) and imports them.</summary>
    [RelayCommand]
    private void SearchSongsOnline()
    {
        var imported = _webSongSearch.Show();
        if (imported.Count == 0)
            return;

        // Clear the filter so the new song is in the list to be selected.
        SearchText = string.Empty;
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == imported[^1].Id) ?? SelectedSong;
        StatusText = imported.Count == 1
            ? $"«{imported[0].Title}» importada de internet."
            : $"{imported.Count} canciones importadas de internet.";
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
            FilterBooks(null);
            BibleStatusText = BibleHint;
            UpdateBibleViewState();
            return;
        }

        // "?" asks for the words inside the verses. Everything else is taken
        // as a book, chapter and verse: that is what gets typed nine times
        // out of ten, and searching the text for "Juan" was never the point.
        var query = BibleQuery.Trim();
        if (query.StartsWith('?'))
        {
            FilterBooks(null);
            SearchBibleText(query[1..].Trim());
            UpdateBibleViewState();
            return;
        }

        var reference = BibleReferenceParser.TryParse(BibleQuery);
        if (reference is not null)
        {
            FilterBooks(null);
            UpdateBibleViewState();

            // Park the left panel on the referenced book (chapter grid view).
            if (SelectedBibleBook?.Number != reference.BookNumber)
                SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == reference.BookNumber);

            // Always load the whole chapter; the referenced verse becomes
            // the "next up" selection, projected with Enter.
            LoadBiblePassage(new BibleReference(reference.BookNumber, reference.Chapter, null, null));

            if (reference.VerseStart is int verse)
            {
                var index = FindVerseSlide(reference.Chapter, verse);
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

        // Halfway through a reference ("juan 3:" before the verse): leave the
        // chapter grid and the slides as they are, the next key finishes it.
        if (System.Text.RegularExpressions.Regex.IsMatch(query, @"\p{L}.*\d"))
        {
            var bookPart = System.Text.RegularExpressions.Regex.Match(query, @"^\s*(\d\s*)?\p{L}[\p{L}\s.]*").Value;
            BibleStatusText = BibleBookCatalog.FindByName(bookPart) is null
                ? $"No reconozco el libro «{bookPart.Trim()}»."
                : "Seguí escribiendo: «juan 3 16» o «juan 3:16».";
            UpdateBibleViewState();
            return;
        }

        // Not a full reference yet: narrow the book list to what fits. A
        // single fit opens its chapters straight away, so "juan" already
        // shows Juan's chapters before the number is typed.
        var fits = FilterBooks(query);
        if (fits.Count == 1)
        {
            if (SelectedBibleBook?.Number != fits[0].Number)
                SelectedBibleBook = fits[0];
            BibleStatusText = $"{fits[0].Name}: escribí el capítulo («{fits[0].Name} 3») o elegilo abajo.";
        }
        else
        {
            SelectedBibleBook = null;
            BibleStatusText = fits.Count == 0
                ? $"Ningún libro empieza con «{query}». Para buscar esa palabra en los versículos: ?{query} · Ctrl+K busca en todo."
                : $"{fits.Count} libros · Enter abre {fits[0].Name}.";
        }

        UpdateBibleViewState();
    }

    private const string BibleHint =
        "Libro, capítulo y versículo: «juan 3 16», «sal 23». Con ? adelante busca en el texto (?paz). Ctrl+K busca en todo.";

    private void SearchBibleText(string words)
    {
        if (PrimaryVersion is null)
            return;

        if (words.Length < 3)
        {
            BibleStatusText = "Escribí al menos 3 letras después del ? para buscar en el texto.";
            return;
        }

        foreach (var result in _bibles.SearchText(PrimaryVersion.Id, words))
            BibleSearchResults.Add(result);

        BibleStatusText = BibleSearchResults.Count == 0
            ? $"Ningún versículo dice «{words}»."
            : $"{BibleSearchResults.Count} versículo(s) con «{words}». Enter proyecta el primero, doble clic cualquiera.";
    }

    /// <summary>
    /// Shows only the books that the typed text could be the start of; null
    /// shows them all again. Returns the books that fit, in biblical order.
    /// </summary>
    private List<BibleBookInfo> FilterBooks(string? typed)
    {
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(BibleBooksAvailable);
        if (string.IsNullOrWhiteSpace(typed))
        {
            if (view.Filter is not null)
                view.Filter = null;
            return BibleBooksAvailable.ToList();
        }

        view.Filter = item => item is BibleBookInfo book && BibleBookCatalog.StartsWith(book, typed);
        return BibleBooksAvailable.Where(b => BibleBookCatalog.StartsWith(b, typed)).ToList();
    }

    private void LoadBiblePassage(BibleReference reference)
    {
        var primary = PrimaryVersion;
        if (primary is null)
            return;

        NoteAction($"Abrir el capítulo {BibleBookCatalog.FindByNumber(reference.BookNumber)?.Name} {reference.Chapter} ({primary.Abbreviation})");
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

        // The grid stops showing the song, so the song list stops pointing at
        // it: otherwise clicking that same song again changes nothing and the
        // chapter stays on screen.
        if (SelectedSong is not null)
            SelectedSong = null;

        _currentPassage = reference;
        Slides.Clear();
        LiveSlideIndex = -1;
        PreviewSlideIndex = -1;

        // Loading a passage by hand steps out of the service order, the same
        // way choosing a song does. Otherwise the end of the playlist's verse
        // range kept jumping to the next item while the operator was just
        // reading through the chapter on their own.
        if (!_openingPlaylistItem)
            _currentPlaylistIndex = -1;

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

        var bookName = verses[0].BookName;
        PreviewMedia = null;
        GridTitle = secondary is null
            ? $"{bookName} {reference.Chapter} · {primary.Abbreviation}"
            : $"{bookName} {reference.Chapter} · {primary.Abbreviation} + {secondary.Abbreviation}";

        foreach (var group in VerseGrouping.Group(verses, secondaryTexts))
        {
            var mainText = theme.ShowVerseNumbers ? $"{group.Numbers}  {group.Text}" : group.Text;
            var verseReference = $"{bookName} {group.Chapter}:{group.Numbers}";

            // Two versions: label each text inline with its abbreviation (like
            // BibleShow) and drop the version(s) from the reference.
            Func<string, string> label = text => text;
            Func<string, string> secondaryLabel = text => text;
            string caption;
            if (secondary is not null)
            {
                label = text => $"[{primary.Abbreviation}] {text}";
                secondaryLabel = text => $"[{secondary.Abbreviation}] {text}";
                caption = verseReference;
            }
            else
            {
                caption = theme.ShowVersionName ? $"{verseReference} · {primary.Abbreviation}" : verseReference;
            }

            // A verse too long to read in one slide goes over several,
            // rather than shrinking past the theme's minimum.
            var parts = Controls.SlideTextFit.SplitToFit(
                new SlideContent(mainText, caption, group.SecondaryText, theme), label, secondaryLabel);

            for (var part = 0; part < parts.Count; part++)
            {
                var slideLabel = $"{group.Chapter}:{group.Numbers}";
                if (parts.Count > 1)
                    slideLabel += $" ({part + 1}/{parts.Count})";

                Slides.Add(new SlideItemViewModel(
                    Slides.Count,
                    slideLabel,
                    new SlideContent(parts[part].Main, caption, parts[part].Secondary, theme))
                {
                    Verses = group,
                });
            }
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
        var query = (BibleQuery ?? string.Empty).Trim();

        // "?words": Enter projects the first verse that says them.
        if (query.StartsWith('?'))
        {
            if (BibleSearchResults.FirstOrDefault() is { } firstResult)
                ProjectBibleResult(firstResult);
            return;
        }

        // Only part of a book typed ("ju"): Enter opens the first book that
        // fits and leaves its name in the box, ready for the chapter number.
        if (query.Length > 0 && BibleReferenceParser.TryParse(query) is null)
        {
            var book = BibleBooksAvailable.FirstOrDefault(b => BibleBookCatalog.StartsWith(b, query));
            if (book is not null)
                BibleQuery = book.Name + " ";
            return;
        }

        // The box can still hold a reference while the grid shows a song the
        // operator went to since. Typing the same text again doesn't reload
        // it, so Enter would have projected a slide of the song.
        if (BibleReferenceParser.TryParse(BibleQuery ?? string.Empty) is { } typed
            && (_currentPassage is null
                || _currentPassage.BookNumber != typed.BookNumber
                || _currentPassage.Chapter != typed.Chapter))
            RunBibleQuery();

        if (PreviewSlideIndex >= 0)
        {
            GoLiveSlide(PreviewSlideIndex);
            return;
        }

        // Going live consumed the preview, so a second Enter lands here.
        // Re-derive the verse from the typed reference: without this it fell
        // back to the chapter's first verse (Enter, Enter projected 3:1).
        var reference = BibleReferenceParser.TryParse(BibleQuery ?? string.Empty);
        if (reference?.VerseStart is int verse)
        {
            var index = FindVerseSlide(reference.Chapter, verse);
            if (index >= 0)
            {
                GoLiveSlide(index);
                return;
            }
        }

        if (Slides.FirstOrDefault(s => s.JumpTarget is null) is { } first)
            GoLiveSlide(first.Index);
    }

    // ── Resaltado en vivo ────────────────────────────────────────

    partial void OnHighlightTextChanged(string value) => _presentation.SetHighlight(value);

    [RelayCommand]
    private void ClearHighlight() => HighlightText = string.Empty;

    /// <summary>
    /// A new slide on screen drops the highlight (the presentation does it),
    /// so the box in the Resaltar tab empties too instead of showing a word
    /// that is no longer marked anywhere.
    /// </summary>
    private void SyncHighlightBox()
    {
        if (_presentation.HighlightTerm is null && HighlightText.Length > 0)
            HighlightText = string.Empty;
    }

    [RelayCommand]
    private void ProjectBibleResult(BibleVerseResult? result)
    {
        if (result is null)
            return;

        LoadBiblePassage(new BibleReference(result.BookNumber, result.Chapter, null, null));
        var index = FindVerseSlide(result.Chapter, result.Verse);
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

        var answer = _textPrompt.AskTwo(
            "Editar versión",
            "Nombre:", target.Name,
            "Abreviatura (la que sale en la referencia, por ejemplo RVR1960):", target.Abbreviation);
        if (answer is not var (name, abbreviation))
            return;

        name = string.IsNullOrWhiteSpace(name) ? target.Name : name;
        abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? target.Abbreviation : abbreviation;
        if (name == target.Name && abbreviation == target.Abbreviation)
            return;

        _bibles.UpdateVersion(target.Id, name, abbreviation);
        LoadBibleVersions();

        // The playlist captions carry the abbreviation, and the slides on the
        // grid show it in their reference: both are read again.
        if (abbreviation != target.Abbreviation)
        {
            LoadPlaylists(null);
            if (_currentPassage is not null)
                RebuildSlidesPreservingLive(() => LoadBiblePassage(_currentPassage));
        }

        BibleStatusText = "Versión actualizada.";
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
    private void NextSlide() => Navigate(+1, isRepeat: false);

    [RelayCommand]
    private void PreviousSlide() => Navigate(-1, isRepeat: false);

    private readonly ArrowStepGate _arrowGate = new();

    /// <summary>
    /// One arrow press. A press that comes from a key held down (the keyboard's
    /// own repetition) never leaves the playlist item: it stops at the edge and
    /// says what another press does, so a held key can't run through the whole
    /// service, putting backgrounds on as it goes.
    /// </summary>
    public void Navigate(int direction, bool isRepeat)
    {
        var neighbour = _currentPlaylistIndex + direction;
        var hasNeighbour = _currentPlaylistIndex >= 0 && neighbour >= 0 && neighbour < PlaylistItems.Count;

        // A media item has no slides of its own: the arrows move between
        // playlist items (the slide grid still shows the previous item), so
        // every press is at the edge.
        var target = LiveSlideIndex < 0 && direction > 0 ? 0 : LiveSlideIndex + direction;
        var atEdge = CurrentPlaylistItemIsMedia
            || target < 0 || target >= Slides.Count || LeavesPassage(direction);

        // At the end of the item, the arrow continues to the next playlist
        // item. For a Bible passage the item ends at its last planned verse,
        // not at the chapter's end (the whole chapter is loaded around it).
        // When there is no next item, the operator can keep reading past the
        // passage.
        switch (_arrowGate.Decide(atEdge, hasNeighbour, isRepeat))
        {
            case ArrowAction.Ignore:
                return;

            case ArrowAction.Hold:
                StatusText = EdgeMessage(direction, PlaylistItems[neighbour]);
                return;

            case ArrowAction.Cross:
                TryAdvancePlaylist(direction);
                return;

            default:
                if (!CurrentPlaylistItemIsMedia)
                    GoLiveSlide(target);
                return;
        }
    }

    /// <summary>What the status bar says when an arrow stops at the edge of an item.</summary>
    private string EdgeMessage(int direction, PlaylistItem neighbour)
    {
        var current = _currentPlaylistIndex >= 0 && _currentPlaylistIndex < PlaylistItems.Count
            ? PlaylistItems[_currentPlaylistIndex].Type
            : PlaylistItemType.Media;
        var edge = (direction > 0 ? "Fin" : "Principio") + current switch
        {
            PlaylistItemType.Song => " de la canción",
            PlaylistItemType.BiblePassage => " del pasaje",
            _ => " del elemento",
        };
        var arrow = direction > 0 ? "→" : "←";
        var verb = direction > 0 ? "pasa a" : "vuelve a";
        return $"{edge}: {arrow} otra vez {verb} «{neighbour.Caption}».";
    }

    private bool CurrentPlaylistItemIsMedia =>
        _currentPlaylistIndex >= 0
        && _currentPlaylistIndex < PlaylistItems.Count
        && PlaylistItems[_currentPlaylistIndex].Type == PlaylistItemType.Media;

    /// <summary>
    /// True when the live slide sits on the edge of the current playlist
    /// item's verse range and the next arrow press in this direction would
    /// step outside the planned passage.
    /// </summary>
    private bool LeavesPassage(int direction)
    {
        if (_currentPlaylistIndex < 0 || _currentPlaylistIndex >= PlaylistItems.Count)
            return false;

        // The edge in this direction is where entering from the opposite
        // direction would land (forward exit = VerseEnd, backward = VerseStart).
        var edge = PassageBoundarySlideIndex(PlaylistItems[_currentPlaylistIndex], -direction);
        return edge is int index && LiveSlideIndex == index;
    }

    /// <summary>Moves to the adjacent playlist item and projects it. False if not applicable.</summary>
    private bool TryAdvancePlaylist(int direction)
    {
        if (_currentPlaylistIndex < 0)
            return false;

        var target = _currentPlaylistIndex + direction;
        if (target < 0 || target >= PlaylistItems.Count)
            return false;

        var item = PlaylistItems[target];
        LoadPlaylistItem(item, projectMedia: true);

        // A media item projects itself when opened (ApplyBackground); putting a
        // slide live on top of it would paint the previous item's text over it.
        if (item.Type != PlaylistItemType.Media)
        {
            // Forward lands on the first slide, backward on the last, so
            // reading through the service is continuous in both directions. A
            // Bible passage bounds that to its own verse range (its item loads
            // the whole chapter; landing on verse 1 would be wrong).
            var projectable = Slides.Where(s => s.JumpTarget is null).ToList();
            var landing = PassageBoundarySlideIndex(item, direction)
                ?? (projectable.Count > 0
                    ? (direction > 0 ? projectable[0].Index : projectable[^1].Index)
                    : null);
            if (landing is int index)
                GoLiveSlide(index);
        }

        StatusText = $"Playlist: {item.Caption}";
        return true;
    }

    /// <summary>
    /// The slide to land on when arrowing into a Bible-passage playlist item:
    /// its first verse going forward, its last verse going backward. Null for
    /// other item types (or when the verse isn't in the loaded chapter).
    /// </summary>
    private int? PassageBoundarySlideIndex(PlaylistItem item, int direction)
    {
        if (item.Type != PlaylistItemType.BiblePassage || item.Chapter is not int chapter)
            return null;

        var verse = direction > 0 ? item.VerseStart : item.VerseEnd ?? item.VerseStart;
        if (verse is not int number)
            return null;

        var index = FindVerseSlide(chapter, number, lastPart: direction < 0);
        return index >= 0 ? index : null;
    }

    /// <summary>
    /// The slide showing a verse of the loaded chapter, or -1. A verse can
    /// share its slide with others (a paragraph of the TLA) or be split over
    /// several: then it is the first part, or the last one going backward.
    /// </summary>
    private int FindVerseSlide(int chapter, int verse, bool lastPart = false)
    {
        var matches = Slides.Where(s => s.Verses is { } v && v.Chapter == chapter && v.Contains(verse));
        return (lastPart ? matches.LastOrDefault() : matches.FirstOrDefault())?.Index ?? -1;
    }

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
        NoteAction($"Proyectar la diapositiva {index + 1} de {Slides.Count}: {item.Label}");

        // A foreground media takes the whole screen and hides the text: it is
        // an announcement, not a background. Projecting lyrics on top of one
        // means the operator is done with it, so it comes off.
        if (_presentation.Background is { Behavior: MediaBehavior.Foreground } foreground
            && item.JumpTarget is null)
        {
            _presentation.SetBackground(null);
            Log.Debug("Fondo de primer plano «{Name}» retirado al proyectar texto", foreground.Name);
        }

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
        SyncHighlightBox();
        LiveTitle = GridTitle;
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        // A song reaching the screen may draw its own background. Done after
        // going live so the slide already exists and the output doesn't dip
        // into "background only" on the way.
        if (item.SectionId != 0)
        {
            ApplyRandomBackgroundForSong();
            NoteSongProjected();
        }

        LiveSlideIndex = index;
        PreviewSlideIndex = -1;
        foreach (var slide in Slides)
        {
            slide.IsLive = slide.Index == index;
            slide.IsPreviewed = false;
        }

        PreviewSlide = index + 1 < Slides.Count ? Slides[index + 1].Slide : null;

        // The stage display needs the label of what is live and the text of
        // what follows, skipping the Bible's chapter-jump cards.
        Projection.SlideLabel = item.Label;
        Projection.NextSlide = Slides
            .Skip(index + 1)
            .FirstOrDefault(s => s.JumpTarget is null)?.Slide;

        Log.Debug("Slide {Index} en vivo: {Label}", index, item.Label);
        StatusText = $"En vivo: diapositiva {index + 1} de {Slides.Count}. Flechas ←→ para navegar · F1 Black · F2 Clear · F3 Logo.";
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
        SyncHighlightBox();
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        LiveSlideIndex = -1;
        foreach (var slide in Slides)
            slide.IsLive = false;

        // Quick text stands alone: nothing follows it on the stage display.
        Projection.SlideLabel = "Texto rápido";
        LiveTitle = "Texto rápido";
        Projection.NextSlide = null;

        StatusText = "Texto rápido en vivo. F1 Black · F2 Clear · F3 Logo · Esc apaga la salida.";
    }

    // ── Notas para la plataforma ─────────────────────────────────

    /// <summary>
    /// The preacher's outline. It only ever reaches the stage display, so
    /// unlike everything else in this panel it does not need the projector on.
    /// </summary>
    [RelayCommand]
    private void ShowStageNotes()
    {
        if (string.IsNullOrWhiteSpace(StageNotesText))
            return;

        var notes = StageNotesText.Trim();
        _presentation.SetStageNotes(notes);
        _settings.Set(StageNotesKey, notes);

        StatusText = IsStageVisible
            ? "Notas en la pantalla de escenario."
            : "Notas guardadas. Se ven al prender la pantalla de escenario (🎭).";
    }

    [RelayCommand]
    private void HideStageNotes()
    {
        _presentation.SetStageNotes(null);
        StatusText = "Notas fuera del escenario.";
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
        NoteAction("Pantalla negra (Black)");
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
        IsCountdownRunning = _presentation.Countdown is not null;
        IsStageNotesActive = _presentation.StageNotes is not null;
        HasBackground = _presentation.Background is not null;

        // Show the blur the current background carries, without saving it back.
        var blur = _presentation.Background?.Blur ?? 0;
        if (Math.Abs(BackgroundBlur - blur) > 0.01)
            BackgroundBlur = blur;

        SyncPlaybackTimer();
    }
}
