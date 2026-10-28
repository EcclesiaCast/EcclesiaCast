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
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>A display plus the label shown to the operator.</summary>
public sealed record DisplayOption(DisplayInfo Info, string Label)
{
    /// <summary>The «only the stream» entry: going live with it opens no output window.</summary>
    public bool BroadcastOnly => BroadcastOnlyOutput.IsBroadcastOnly(Info);
}

public sealed partial class MainViewModel : ObservableObject, IRemoteHost
{
    private const string OutputDisplayKey = "output.display";
    private const string StageNotesKey = "stage.notes.text";
    private const string QuickTextFontKey = "quicktext.font";
    private const string BibleHistoryKey = "bible.history";
    private const string CountdownMinutesKey = "countdown.minutes";
    private const string CountdownClockKey = "countdown.clock";
    private const string CountdownHeadingKey = "countdown.heading";
    private const string CountdownFinishedKey = "countdown.finished";

    /// <summary>Marks that video posters were rebuilt with the VLC fallback (see RefreshVideoThumbnailsAsync).</summary>
    private const string VideoThumbnailsRebuiltKey = "media.video-thumbnails.rebuilt";

    /// <summary>
    /// Marks that the posters of images and document pages were redrawn in
    /// their own shape (they used to be whatever the Windows shell gave).
    /// </summary>
    private const string ImageThumbnailsShapedKey = "media.image-thumbnails.shaped";

    private readonly IDisplayProvider _displayProvider;
    private readonly IProjectionWindowService _projection;
    private readonly ISettingsStore _settings;
    private readonly IPresentationService _presentation;
    private readonly ISongRepository _songs;
    private readonly ISongEditor _songEditor;
    private readonly IBibleRepository _bibles;
    private readonly IBibleImportDialog _bibleImportDialog;
    private readonly ITextPrompt _textPrompt;
    private readonly SongSaver _songSaver;
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

    /// <summary>The verses projected from the search, newest first (the 🕘 list next to the search box).</summary>
    public ObservableCollection<BibleHistoryEntry> BibleHistory { get; } = [];

    /// <summary>Books present in the primary version, in Bible order.</summary>
    public ObservableCollection<BibleBookInfo> BibleBooksAvailable { get; } = [];

    /// <summary>Chapters available for the selected book — the numbered button grid.</summary>
    public ObservableCollection<ChapterOption> BibleChapters { get; } = [];

    /// <summary>Ids of the checked versions, oldest first; index 0 is the primary.</summary>
    private readonly List<int> _checkedVersionIds = [];

    private bool _suppressVersionEvents;

    private const string BibleVersionsExpandedKey = "bible.versions.expanded";

    /// <summary>The versions row is open into a list with the full names.</summary>
    [ObservableProperty]
    private bool _isBibleVersionsExpanded;

    partial void OnIsBibleVersionsExpandedChanged(bool value) =>
        _settings.Set(BibleVersionsExpandedKey, value ? "1" : "0");

    /// <summary>The passage currently shown in the grid, to re-render when versions change.</summary>
    private BibleReference? _currentPassage;

    /// <summary>The center slide grid — filled from either a song or a Bible passage.</summary>
    public ObservableCollection<SlideItemViewModel> Slides { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputAspectRatio), nameof(IsBroadcastOnly), nameof(IsBroadcastOnlyLive))]
    private DisplayOption? _selectedDisplay;

    /// <summary>The output is set to «only the stream»: no window opens, the pages for OBS carry the words.</summary>
    public bool IsBroadcastOnly => SelectedDisplay?.BroadcastOnly == true;

    /// <summary>Live with no output window: what the status bar says in red.</summary>
    public bool IsBroadcastOnlyLive => IsProjecting && IsBroadcastOnly;

    /// <summary>Width over height of the output screen: LIVE and PREVIEW are drawn at this proportion.</summary>
    public double OutputAspectRatio => SelectedDisplay is { Info: var info }
        ? AspectFit.RatioOf(info.Width, info.Height)
        : AspectFit.DefaultRatio;

    [ObservableProperty]
    private string _statusText =
        "Creá una canción con ➕, o escribí un texto rápido y presioná Ctrl+Enter.";

    /// <summary>Goes up by one every time something went wrong that the operator has to see: the window makes the status bar flash.</summary>
    [ObservableProperty]
    private int _statusAlert;

    /// <summary>Says what went wrong in the status bar and makes it flash red, because a message nobody notices is a failure nobody hears about.</summary>
    private void ShowStatusError(string message)
    {
        StatusText = message;
        StatusAlert++;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGridLiveBadge), nameof(ShowOtherLive), nameof(IsBroadcastOnlyLive))]
    private bool _isProjecting;

    [ObservableProperty]
    private string _quickText = string.Empty;

    /// <summary>Typeface of the quick text. Empty keeps the songs' one from the theme.</summary>
    [ObservableProperty]
    private string _quickTextFont = string.Empty;

    /// <summary>The installed typefaces, for the quick text's list, in alphabetical order.</summary>
    public IReadOnlyList<string> SystemFonts { get; } =
        System.Windows.Media.Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList();

    /// <summary>What is being prepared; the Preview box binds to it.</summary>
    [ObservableProperty]
    private SlideContent? _previewSlide;

    [ObservableProperty]
    private string _overlayText = string.Empty;

    /// <summary>Typeface of the lower-third announcement. Empty keeps Segoe UI.</summary>
    [ObservableProperty]
    private string _overlayFont = string.Empty;

    /// <summary>Size of the lower-third announcement's letters (20 to 160).</summary>
    [ObservableProperty]
    private double _overlayFontSize = OverlayStyle.DefaultSize;

    partial void OnOverlayFontChanged(string value)
    {
        if (!_loadingOverlayStyle)
            _settings.Set(OverlayStyle.FontSettingKey, value);
        Projection.OverlayFontFamily = OverlayStyle.FontOrDefault(value);
    }

    partial void OnOverlayFontSizeChanged(double value)
    {
        var size = OverlayStyle.ClampSize(value);
        if (!_loadingOverlayStyle)
            _settings.Set(OverlayStyle.SizeSettingKey, OverlayStyle.FormatSize(size));
        Projection.OverlayFontSize = size;
    }

    private bool _loadingOverlayStyle;

    private void LoadOverlayStyle()
    {
        _loadingOverlayStyle = true;
        OverlayFont = _settings.Get(OverlayStyle.FontSettingKey) ?? string.Empty;
        OverlayFontSize = OverlayStyle.ParseSize(_settings.Get(OverlayStyle.SizeSettingKey));
        _loadingOverlayStyle = false;
    }

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
    [NotifyPropertyChangedFor(nameof(IsGridLive), nameof(HasGridTitle), nameof(IsGridOnAir), nameof(ShowGridLiveBadge), nameof(ShowOtherLive))]
    private string? _gridTitle;

    /// <summary>The name of what was last put on screen, by the same naming.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridLive), nameof(IsGridOnAir), nameof(ShowGridLiveBadge), nameof(ShowOtherLive))]
    private string? _liveTitle;

    /// <summary>True when the grid shows the very thing that was last put on screen (by name).</summary>
    public bool IsGridLive => LiveTitle is not null && LiveTitle == GridTitle;

    public bool HasGridTitle => !string.IsNullOrEmpty(GridTitle);

    /// <summary>
    /// The «EN VIVO» mark next to the grid's title: only while what is on
    /// screen really is what the grid holds (see <see cref="IsGridOnAir"/>).
    /// </summary>
    public bool ShowGridLiveBadge => IsGridOnAir;

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
        SongSaver songSaver,
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
        IGlobalSearchDialog globalSearch,
        IAudioRepository audios,
        IAudioPlayer audioPlayer,
        IUpdates updates,
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
        _songSaver = songSaver;
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
        _globalSearch = globalSearch;
        _audios = audios;
        _audioPlayer = audioPlayer;
        _updates = updates;
        Projection = projectionViewModel;

        _presentation.Changed += (_, _) => UpdateStateFlags();
        _playbackTimer.Tick += OnPlaybackTick;
        // With «only the stream» there is no window to follow: going live is
        // the program's own state, and a window closing as the choice changes
        // must not switch it off.
        _projection.VisibilityChanged += (_, _) =>
        {
            if (!IsBroadcastOnly)
                IsProjecting = _projection.IsOutputVisible;
        };
        _projection.VideoEnded += (_, _) => OnProjectedVideoEnded();
        _stage.VisibilityChanged += (_, _) => IsStageVisible = _stage.IsVisible;
        LoadStageLayouts();
        LoadSongMaxLines();
        LoadLowPerformance();
        LoadMeetingSections();
        Slides.CollectionChanged += (_, _) =>
        {
            HasSlides = Slides.Count > 0;
            IsSongOpen = Slides.Any(s => s.SectionId != 0);
        };
        UpdateStateFlags();
        RefreshDisplays();
        LoadSongs();
        IsBibleVersionsExpanded = _settings.Get(BibleVersionsExpandedKey) == "1";

        // The Bibles the installer left, before anything reads the library:
        // the window is not up yet, so there is no one to keep waiting.
        var bundledNote = ImportBundledBibles();
        var backgroundsNote = ImportBundledBackgrounds();
        LoadBibleVersions();
        LoadBibleHistory();
        LoadMedia();
        LoadLogos(null);
        LoadPlaylists(null);
        InitializeAudio();
        InitializeWeb();
        InitializeCardZoom();
        InitializeUpdates();
        StageNotesText = _settings.Get(StageNotesKey) ?? string.Empty;
        QuickTextFont = _settings.Get(QuickTextFontKey) ?? string.Empty;
        LoadOverlayStyle();
        _previewShowsStage = _settings.Get(PreviewShowsStageKey) == "1";
        RestoreRemote();
        _ = RefreshVideoThumbnailsAsync();

        // Light copies of the heavy videos, made in the background; the
        // status bar says when one is being prepared or is ready.
        LightVideoCache.StatusChanged += message =>
            Application.Current?.Dispatcher.BeginInvoke(() => StatusText = message);
        RefreshLightCopies();

        if (bundledNote is not null || backgroundsNote is not null)
            StatusText = string.Join(" ", new[] { bundledNote, backgroundsNote }.OfType<string>());
    }
}
