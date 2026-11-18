using System.IO;
using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.App.Platform;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Feedback;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Data.Persistence;
using EcclesiaCast.Platform.Windows;
using LibVLCSharp.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EcclesiaCast.App;

public partial class App : Application
{
    private ServiceProvider? _services;
    private UiWatchdog? _watchdog;
    private CrashMarker? _crashMarker;
    private readonly EcclesiaCast.Core.Diagnostics.ErrorNoticeGate _errorGate = new(TimeSpan.FromSeconds(30));

    /// <summary>Set once the "Acerca de" of the very first start has been shown.</summary>
    private const string AboutSeenKey = "app.about.seen";

    // The platform services, made first: the static helpers (Notice, the
    // importers, the sound watcher) and App.DataDir reach them from here on.
    private readonly WpfUiDispatcher _uiDispatcher;
    private readonly WpfUiTimerFactory _uiTimers = new();
    private readonly WpfNotices _notices = new();
    private readonly WindowsPlatformPaths _paths = new();

    public App()
    {
        _uiDispatcher = new WpfUiDispatcher(Dispatcher);
        PlatformServices.Dispatcher = _uiDispatcher;
        PlatformServices.Timers = _uiTimers;
        PlatformServices.Notices = _notices;
        PlatformServices.Paths = _paths;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Dark title bar for every window (main and dialogs).
        EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
            new RoutedEventHandler((s, _) => DarkTitleBar.Apply((Window)s)));

        // Tooltips explain what each button does, so they have to stay up long
        // enough to actually read them (WPF hides them after five seconds).
        ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(DependencyObject), new FrameworkPropertyMetadata(30000));
        ToolTipService.InitialShowDelayProperty.OverrideMetadata(
            typeof(DependencyObject), new FrameworkPropertyMetadata(350));

        // ECCLESIACAST_DATA_DIR points a test build at a copy of a library,
        // so it can run next to the real one without touching it.
        var appDataDir = DataDir;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(appDataDir, "logs", "ecclesiacast-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        // The loading screen goes up first, before the slow parts (database,
        // video engine), and comes down when the main window has been drawn.
        var splash = new Views.SplashWindow();
        splash.ShowNow();

        Log.Information("EcclesiaCast starting");

        PowerThrottling.OptOut();

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception");
            if (_services?.GetService<MainViewModel>() is { } vm && MainWindow is { IsLoaded: true })
            {
                // The operator keeps going: no dialog, a line in the status bar, at most every 30 s.
                if (_errorGate.ShouldNotify(DateTimeOffset.Now))
                    vm.StatusText = "Hubo un error; el programa sigue. Quedó anotado en el registro.";
            }
            else
            {
                // Start-up error: nobody is using the program yet, so a dialog is fine.
                Notice.Show(
                    args.Exception.Message,
                    "Ocurrió un error inesperado",
                    NoticeKind.Error);
            }
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Fatal unhandled exception (terminating: {Terminating})", args.IsTerminating);
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        var dbPath = Path.Combine(appDataDir, "ecclesiacast.db");
        Directory.CreateDirectory(appDataDir);

        // A restore chosen in the previous run is swapped in here, before
        // anything opens the database — that is the only moment nothing holds
        // it. Migrations then bring an older library up to date by themselves.
        var restore = DatabaseBackup.ApplyPendingRestore(dbPath);
        if (restore.Restored)
        {
            Log.Information("Biblioteca restaurada desde una copia de seguridad");
            Notice.Show(
                "Por las dudas, la biblioteca que tenías antes quedó guardada en:\n\n"
                + DatabaseBackup.ReplacedLibraryPath(dbPath),
                "Se restauró la copia de seguridad");
        }
        else if (restore.WasStaged)
        {
            Log.Error("No se pudo aplicar la restauración pendiente: {Error}", restore.Error);
            Notice.Show(
                "Suele ser otra ventana de EcclesiaCast abierta. Cerralas todas y volvé a abrir el "
                + "programa; la copia sigue esperando y se restaura sola.\n\n"
                + $"Detalle: {restore.Error}",
                "No se pudo restaurar la copia: algo está usando la biblioteca",
                NoticeKind.Warning);
        }

        using (var db = new AppDbContext(dbPath))
            db.Database.Migrate();

        // Temas iniciales (Canciones y Biblia) en la primera ejecución.
        // What has been in the trash for more than 30 days goes for good.
        var purgeLimit = EcclesiaCast.Core.Songs.TrashPolicy.PurgeLimit(DateTime.UtcNow);
        new SongRepository(dbPath).PurgeOlderThan(purgeLimit);
        new ThemeRepository(dbPath).PurgeOlderThan(purgeLimit);

        ThemeSeeder.EnsureDefaults(new ThemeRepository(dbPath), new SqliteSettingsStore(dbPath));

        // Motor de video (VLC) para los fondos en movimiento.
        LibVLC? libVlc = null;
        try
        {
            LibVLCSharp.Shared.Core.Initialize();
            // --no-volume-save: VLC kept its volume and mute in a Windows
            // audio session of its own that Windows remembers between runs, so
            // a mute left there by an older version stayed on for good. Each
            // player now sets its own level (PlayerSound) and the session
            // opens at full every time.
            libVlc = new LibVLC("--no-osd", "--quiet", "--no-volume-save");
            VlcLog.Attach(libVlc);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo inicializar LibVLC; los videos de fondo quedan deshabilitados");
        }

        var videoEngine = new VideoEngine(libVlc);
        PlatformServices.Video = videoEngine;

        // Hears when Windows changes the audio device, so the players can open
        // their sound output again instead of staying mute until a restart.
        AudioEndpointWatcher.Start();

        var services = new ServiceCollection();
        services.AddSingleton<IUiDispatcher>(_uiDispatcher);
        services.AddSingleton<IUiTimerFactory>(_uiTimers);
        services.AddSingleton<INotices>(_notices);
        services.AddSingleton<IPlatformPaths>(_paths);
        services.AddSingleton<IVideoEngine>(videoEngine);
        services.AddSingleton<IDialogs, WpfDialogs>();
        services.AddSingleton<IImageFactory, WpfImageFactory>();
        services.AddSingleton<IFontCatalog, WpfFontCatalog>();
        services.AddSingleton<IClipboardText, WpfClipboardText>();
        services.AddSingleton<IShell, WindowsShell>();
        services.AddSingleton<IPlatformInfo, WindowsPlatformInfo>();
        services.AddSingleton<IDisplayProvider, ScreenDisplayProvider>();
        services.AddSingleton<IPresentationService, PresentationService>();
        services.AddSingleton<EcclesiaCast.Core.Timing.MeetingClocks>();
        services.AddSingleton<ProjectionViewModel>();
        services.AddSingleton<IProjectionWindowService, ProjectionWindowService>();
        services.AddSingleton<ISettingsStore>(_ => new SqliteSettingsStore(dbPath));
        services.AddSingleton<ISongRepository>(_ => new SongRepository(dbPath));
        services.AddSingleton<ISongEditor, SongEditorService>();
        services.AddSingleton<IBibleRepository>(_ => new BibleRepository(dbPath));
        services.AddSingleton<IBibleImportDialog, BibleImportDialogService>();
        services.AddSingleton<ITextPrompt, TextPromptService>();
        services.AddSingleton<SongSaver>();
        services.AddSingleton<IThemeRepository>(_ => new ThemeRepository(dbPath));
        services.AddSingleton<IThemeManagerDialog, ThemeManagerDialogService>();
        services.AddSingleton<ISongDesigner, SongDesignerService>();
        services.AddSingleton<IQuickTextEditor, QuickTextEditorService>();
        services.AddSingleton<IMediaRepository>(_ => new MediaRepository(dbPath));
        services.AddSingleton<IAudioRepository>(_ => new AudioRepository(dbPath));
        services.AddSingleton<IAudioPlayer>(_ => new VlcAudioPlayer(() => VideoEngine));
        services.AddSingleton<IMediaInspector, MediaInspectorService>();
        services.AddSingleton<IPlaylistRepository>(_ => new PlaylistRepository(dbPath));
        services.AddSingleton<IYouTubeBrowser, YouTubeBrowserService>();
        services.AddSingleton<ILogoRepository>(_ => new LogoRepository(dbPath));
        services.AddSingleton<ILogoManagerDialog, LogoManagerDialogService>();
        services.AddSingleton<ISongImportDialog, SongImportDialogService>();
        services.AddSingleton<IWebSongSearchDialog, WebSongSearchDialogService>();
        services.AddSingleton<IStageWindowService, StageWindowService>();
        services.AddSingleton<IStageDesigner, StageDesignerService>();
        services.AddSingleton<IBackupDialog>(_ => new BackupDialogService(dbPath));
        services.AddSingleton<ICountdownDialog, CountdownDialogService>();
        services.AddSingleton<IGlobalSearchDialog, GlobalSearchDialogService>();
        services.AddSingleton(sp => new UpdateChecker(
            sp.GetRequiredService<ISettingsStore>(),
            sp.GetRequiredService<IProjectionWindowService>(),
            appDataDir,
            dbPath));
        services.AddSingleton<IUpdates>(sp => sp.GetRequiredService<UpdateChecker>());
        services.AddSingleton<MainViewModel>();
        _services = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainViewModel>()
        };
        var settings = _services.GetRequiredService<ISettingsStore>();
        ColorPickerHelper.Settings = settings;
        TransitionSettings.Load(settings);
        BroadcastStyleSettings.Load(settings);
        mainWindow.AttachLayoutPersistence(settings);

        // How much the program is used, and whether the last run ended badly:
        // both only ever leave the computer inside a comment the person sends.
        FeedbackCoordinator.Start(settings, appDataDir);
        var crashMarker = new CrashMarker(appDataDir);
        var lastRunCrashed = crashMarker.Begin();
        _crashMarker = crashMarker;
        SessionEnding += (_, _) => crashMarker.End();

        // The splash was the first window opened, so WPF took it for the main
        // one: hand the title over before closing it, or the program would exit.
        MainWindow = mainWindow;
        mainWindow.ContentRendered += async (_, _) =>
        {
            splash.Close();

            // Once, the first time the program ever opens.
            var firstRun = OneTimeNotice.ShouldShow(settings, AboutSeenKey);
            if (firstRun)
                new Views.AboutWindow(Services.AppInfo.Version, firstRun: true) { Owner = mainWindow }.ShowDialog();

            // A Bible package chosen on the installer's page opens its import window (once).
            var biblePackage = _services.GetRequiredService<MainViewModel>().OfferInstalledBiblePackageAsync();

            // The first time, after those two, the tour of the window. Someone
            // updating from an older version already saw the About window, so
            // it does not open by itself for them (it is in Ayuda).
            if (firstRun)
            {
                try
                {
                    await biblePackage;
                    mainWindow.StartWelcomeTour();
                }
                catch (Exception ex)
                {
                    // A tour that cannot be drawn must not stand in the way.
                    Log.Warning(ex, "The welcome tour could not start");
                }
            }

            // Not on the very first start (the person has not used it yet), and
            // not past the day the temporary notices stop.
            if (lastRunCrashed && DateTime.Now <= FeedbackForm.AutoPromptUntil
                && settings.Get(FeedbackPolicy.NeverKey) != "1" && (FeedbackCoordinator.Opens > 1))
                FeedbackCoordinator.AskAfterCrash(mainWindow);
        };
        mainWindow.Show();

        // Looks for a new version a few seconds from now, not during start-up.
        _services.GetRequiredService<UpdateChecker>().StartAutomaticCheck();

        // Last, so the slow start-up is not mistaken for a freeze.
        _watchdog = new UiWatchdog(Dispatcher);
        _watchdog.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("EcclesiaCast exiting");
        FeedbackCoordinator.RecordEnd();
        _crashMarker?.End();
        _watchdog?.Stop();
        AudioEndpointWatcher.Stop();
        LightVideoCache.Shutdown();

        // Stop listening on the network before the process goes away.
        _services?.GetService<MainViewModel>()?.StopRemote();
        _services?.GetService<MainViewModel>()?.StopAudio();
        _services?.GetService<MainViewModel>()?.ShutDownListening();
        _services?.GetService<MainViewModel>()?.StopWindowCapture();
        Log.CloseAndFlush();
        _services?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Convenience for windows/services that optionally use video. The view
    /// models move to <see cref="IVideoEngine"/> when they leave the app.
    /// </summary>
    public static LibVLC? VideoEngine => PlatformServices.Video?.Instance;

    /// <summary>
    /// Where the library, thumbnails and imported pages live
    /// (<see cref="IPlatformPaths.DataFolder"/>, which follows
    /// ECCLESIACAST_DATA_DIR). The view models move to
    /// <see cref="IPlatformPaths"/> when they leave the app.
    /// </summary>
    public static string DataDir => PlatformServices.Paths.DataFolder;
}
