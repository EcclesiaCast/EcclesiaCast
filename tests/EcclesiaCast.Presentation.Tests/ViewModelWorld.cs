using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Data.Persistence;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Presentation.Tests;

/// <summary>
/// A whole program without a screen: a real library in a temporary folder
/// (the same repositories the program uses), the real presentation service and
/// clocks, and made-up platform services. <see cref="Build"/> makes the
/// MainViewModel the way <c>App.xaml.cs</c> does.
/// </summary>
public sealed class ViewModelWorld : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ecclesiacast-vm-" + Guid.NewGuid().ToString("N"));
    private readonly string _dbPath;

    internal ManualTimers Timers { get; } = new();

    public ISettingsStore Settings { get; }

    public ISongRepository Songs { get; }

    public IBibleRepository Bibles { get; }

    public ViewModelWorld()
    {
        Directory.CreateDirectory(_root);
        _dbPath = Path.Combine(_root, "ecclesiacast.db");

        using (var db = new AppDbContext(_dbPath))
            db.Database.Migrate();

        Settings = new SqliteSettingsStore(_dbPath);
        Songs = new SongRepository(_dbPath);
        Bibles = new BibleRepository(_dbPath);
        ThemeSeeder.EnsureDefaults(new ThemeRepository(_dbPath), Settings);
    }

    /// <summary>Call after putting in the songs, Bibles and settings the test needs: the view model reads them when it is built.</summary>
    public MainViewModel Build()
    {
        var paths = new TempPaths(_root);

        // What each interface sets first thing at start-up, for the services
        // the view model reaches without being handed them.
        PlatformServices.Paths = paths;
        PlatformServices.Dispatcher = new InlineDispatcher();
        PlatformServices.Timers = Timers;
        PlatformServices.Video = Stub.Of<IVideoEngine>();

        var presentation = new PresentationService();
        var songs = Songs;
        var prompt = Stub.Of<ITextPrompt>();

        return new MainViewModel(
            displayProvider: new TwoScreens(),
            projection: Stub.Of<IProjectionWindowService>(),
            settings: Settings,
            presentation: presentation,
            songs: songs,
            songEditor: Stub.Of<ISongEditor>(),
            bibles: Bibles,
            bibleImportDialog: Stub.Of<IBibleImportDialog>(),
            textPrompt: prompt,
            songSaver: new SongSaver(songs, prompt),
            themes: new ThemeRepository(_dbPath),
            themeManager: Stub.Of<IThemeManagerDialog>(),
            songDesigner: Stub.Of<ISongDesigner>(),
            quickTextEditor: Stub.Of<IQuickTextEditor>(),
            media: new MediaRepository(_dbPath),
            mediaInspector: Stub.Of<IMediaInspector>(),
            playlists: new PlaylistRepository(_dbPath),
            youTube: Stub.Of<IYouTubeBrowser>(),
            logos: new LogoRepository(_dbPath),
            logoManager: Stub.Of<ILogoManagerDialog>(),
            songImport: Stub.Of<ISongImportDialog>(),
            webSongSearch: Stub.Of<IWebSongSearchDialog>(),
            stage: Stub.Of<IStageWindowService>(),
            stageDesigner: Stub.Of<IStageDesigner>(),
            backup: Stub.Of<IBackupDialog>(),
            countdownDialog: Stub.Of<ICountdownDialog>(),
            globalSearch: Stub.Of<IGlobalSearchDialog>(),
            audios: new AudioRepository(_dbPath),
            audioPlayer: Stub.Of<IAudioPlayer>(),
            updates: Stub.Of<IUpdates>(),
            projectionViewModel: new ProjectionViewModel(presentation, new MeetingClocks()),
            dispatcher: new InlineDispatcher(),
            fonts: new NoFonts(),
            timers: Timers,
            dialogs: Stub.Of<IDialogs>(),
            paths: paths,
            images: new NoImages(),
            thumbnails: Stub.Of<IThumbnailer>(),
            video: Stub.Of<IVideoEngine>(),
            lightCopy: Stub.Of<ILightCopyDialog>(),
            installer: Stub.Of<IInstallerSettings>(),
            operatorScreen: Stub.Of<IOperatorScreen>(),
            broadcastDialog: Stub.Of<IBroadcastDialog>(),
            remoteDialog: Stub.Of<IRemoteControlDialog>(),
            trashDialog: Stub.Of<ITrashDialog>(),
            webEngine: Stub.Of<IWebEngine>(),
            youTubeDownload: Stub.Of<IYouTubeDownloadDialog>(),
            shell: Stub.Of<IShell>(),
            newWindowCapture: Stub.Of<IWindowCapture>,
            windowPicker: Stub.Of<IWindowPickerDialog>(),
            newListener: Stub.Of<ISongListener>,
            soundInput: Stub.Of<ISoundInput>(),
            speechEar: Stub.Of<ISpeechEar>(),
            biblePackageDialog: Stub.Of<IBiblePackageDialog>(),
            keyboard: new NoKeys(),
            listFilter: new UnfilteredLists(),
            textMeasurer: new FitsInOneSlide(),
            bibleFormatHelp: Stub.Of<IBibleFormatHelpDialog>());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A temporary folder left behind is not worth failing a test.
        }
    }
}
