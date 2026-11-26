using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Data.Persistence;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Platform.Mac;
using EcclesiaCast.Platform.Windows;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using LibVLCSharp.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EcclesiaCast.Desktop;

/// <summary>What only the system the program runs on can give.</summary>
public sealed record DesktopPlatform(
    IPlatformPaths Paths,
    IOutputWindowNative Native,
    IPowerGuard Power,
    IScreenList Screens,
    IOperatorScreen OperatorScreen,
    INotices Notices,
    LibVLC? Video)
{
    /// <summary>
    /// The pieces of this system: those of a Mac (Platform.Mac), or on
    /// Windows the same folders and calls the WPF program uses (their files
    /// are shared with Platform.Windows).
    /// </summary>
    public static DesktopPlatform ForThisSystem(IScreenList screens, IOperatorScreen operatorScreen, INotices notices, LibVLC? video) =>
        OperatingSystem.IsWindows()
            ? new(SystemPaths, new WindowsOutputWindowNative(), new WindowsPowerGuard(),
                screens, operatorScreen, notices, video)
            : new(SystemPaths, new MacOutputWindowNative(), new MacPowerGuard(),
                screens, operatorScreen, notices, video);

    /// <summary>The folders of this system, read once (the log needs them before anything else).</summary>
    public static IPlatformPaths SystemPaths { get; } =
        OperatingSystem.IsWindows() ? new WindowsPlatformPaths() : new MacPlatformPaths();
}

/// <summary>
/// The composition root of the Avalonia program: the real
/// <see cref="MainViewModel"/>, made the way <c>App.xaml.cs</c> makes it in
/// WPF, with the Avalonia platform services. The dialogs phase 3 has not made
/// yet are all served by <see cref="NotOnMacYet"/>.
/// </summary>
public static class Composition
{
    /// <summary>
    /// The library, ready to open: a restore chosen in the previous run
    /// swapped in, the database brought up to date, the trash emptied of
    /// what is older than 30 days and the themes of the first start made.
    /// Returns the path of the database.
    /// </summary>
    public static string PrepareLibrary(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder);
        var dbPath = Path.Combine(dataFolder, "ecclesiacast.db");

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

        var purgeLimit = EcclesiaCast.Core.Songs.TrashPolicy.PurgeLimit(DateTime.UtcNow);
        new SongRepository(dbPath).PurgeOlderThan(purgeLimit);
        new ThemeRepository(dbPath).PurgeOlderThan(purgeLimit);
        ThemeSeeder.EnsureDefaults(new ThemeRepository(dbPath), new SqliteSettingsStore(dbPath));
        return dbPath;
    }

    /// <summary>
    /// Sets the platform services the static helpers reach (first thing, as
    /// WPF does) and builds every service. The caller disposes it.
    /// </summary>
    public static ServiceProvider Build(DesktopPlatform platform, string dbPath)
    {
        var dispatcher = new AvaloniaUiDispatcher();
        var timers = new AvaloniaUiTimerFactory();
        var videoEngine = new VideoEngine(platform.Video);

        PlatformServices.Dispatcher = dispatcher;
        PlatformServices.Timers = timers;
        PlatformServices.Notices = platform.Notices;
        PlatformServices.Paths = platform.Paths;
        PlatformServices.Video = videoEngine;

        var services = new ServiceCollection();
        services.AddSingleton<IUiDispatcher>(dispatcher);
        services.AddSingleton<IUiTimerFactory>(timers);
        services.AddSingleton(platform.Notices);
        services.AddSingleton(platform.Paths);
        services.AddSingleton<IVideoEngine>(videoEngine);
        services.AddSingleton(platform.Native);
        services.AddSingleton(platform.Power);
        services.AddSingleton(platform.Screens);
        services.AddSingleton<IDisplayProvider>(platform.Screens);
        services.AddSingleton(platform.OperatorScreen);
        services.AddSingleton<IImageFactory, AvaloniaImageFactory>();
        services.AddSingleton<IFontCatalog>(AvaloniaFontCatalog.Current);
        services.AddSingleton<AvaloniaClipboardText>();
        services.AddSingleton<IClipboardText>(sp => sp.GetRequiredService<AvaloniaClipboardText>());
        services.AddSingleton<IThumbnailer, AvaloniaThumbnailer>();
        services.AddSingleton<IInstallerSettings, NoInstallerSettings>();
        services.AddSingleton<IWebEngine, NoWebEngine>();
        services.AddSingleton<IShell, DesktopShell>();
        services.AddSingleton<IPlatformInfo, AvaloniaPlatformInfo>();
        services.AddSingleton<AvaloniaKeyboardState>();
        services.AddSingleton<IKeyboardState>(sp => sp.GetRequiredService<AvaloniaKeyboardState>());
        services.AddSingleton<AvaloniaListFilter>();
        services.AddSingleton<IListFilter>(sp => sp.GetRequiredService<AvaloniaListFilter>());
        services.AddSingleton<ITextMeasurer, AvaloniaTextMeasurer>();

        services.AddSingleton<IPresentationService, PresentationService>();
        services.AddSingleton<MeetingClocks>();
        services.AddSingleton<ProjectionViewModel>();
        services.AddSingleton<ProjectionWindowService>();
        services.AddSingleton<IProjectionWindowService>(sp => sp.GetRequiredService<ProjectionWindowService>());
        services.AddSingleton<StageWindowService>();
        services.AddSingleton<IStageWindowService>(sp => sp.GetRequiredService<StageWindowService>());

        services.AddSingleton<ISettingsStore>(_ => new SqliteSettingsStore(dbPath));
        services.AddSingleton<ISongRepository>(_ => new SongRepository(dbPath));
        services.AddSingleton<IBibleRepository>(_ => new BibleRepository(dbPath));
        services.AddSingleton<IThemeRepository>(_ => new ThemeRepository(dbPath));
        services.AddSingleton<IMediaRepository>(_ => new MediaRepository(dbPath));
        services.AddSingleton<IAudioRepository>(_ => new AudioRepository(dbPath));
        services.AddSingleton<IPlaylistRepository>(_ => new PlaylistRepository(dbPath));
        services.AddSingleton<ILogoRepository>(_ => new LogoRepository(dbPath));
        services.AddSingleton<IAudioPlayer>(_ => new VlcAudioPlayer(() => videoEngine.Instance));
        services.AddSingleton<SongSaver>();

        // The phone remote is the one dialog the test program has: it is
        // one of the six things to try on the Mac.
        services.AddSingleton<IRemoteControlDialog, RemoteControlDialog>();

        // Every other dialog: «Esto todavía no está en la versión de Mac».
        services.AddSingleton(_ => NotOnMacYet.For<IDialogs>());
        services.AddSingleton(_ => NotOnMacYet.For<ISongEditor>());
        services.AddSingleton(_ => NotOnMacYet.For<IBibleImportDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<ITextPrompt>());
        services.AddSingleton(_ => NotOnMacYet.For<IThemeManagerDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<ISongDesigner>());
        services.AddSingleton(_ => NotOnMacYet.For<IQuickTextEditor>());
        services.AddSingleton(_ => NotOnMacYet.For<IMediaInspector>());
        services.AddSingleton(_ => NotOnMacYet.For<IYouTubeBrowser>());
        services.AddSingleton(_ => NotOnMacYet.For<ILogoManagerDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<ISongImportDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IWebSongSearchDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IStageDesigner>());
        services.AddSingleton(_ => NotOnMacYet.For<IBackupDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<ICountdownDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<ILightCopyDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IColorPicker>());
        services.AddSingleton(_ => NotOnMacYet.For<IBibleFormatHelpDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IBiblePackageDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IWindowPickerDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IYouTubeDownloadDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<ITrashDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IBroadcastDialog>());
        services.AddSingleton(_ => NotOnMacYet.For<IGlobalSearchDialog>());

        // Functions the Avalonia program never offers (IPlatformInfo says
        // so): answered without a notice.
        services.AddSingleton(_ => NotOnMacYet.Silent<IUpdates>());
        services.AddSingleton(_ => NotOnMacYet.Silent<ISoundInput>());
        services.AddSingleton(_ => NotOnMacYet.Silent<ISpeechEar>());
        services.AddSingleton<Func<ISongListener>>(_ => NotOnMacYet.Silent<ISongListener>);
        services.AddSingleton<Func<IWindowCapture>>(_ => NotOnMacYet.Silent<IWindowCapture>);

        services.AddSingleton<MainViewModel>();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// After the services exist: the settings the WPF start-up loads, and
    /// the operator warned when the screen of the output or the stage goes
    /// away, with the list of screens read again.
    /// </summary>
    public static void Start(IServiceProvider services)
    {
        var settings = services.GetRequiredService<ISettingsStore>();
        TransitionSettings.Load(settings);
        BroadcastStyleSettings.Load(settings);

        var vm = services.GetRequiredService<MainViewModel>();
        var screens = services.GetRequiredService<IScreenList>();
        var projection = services.GetRequiredService<ProjectionWindowService>();
        var stage = services.GetRequiredService<StageWindowService>();
        var dispatcher = services.GetRequiredService<IUiDispatcher>();

        var watch = new ScreenWatch(vm, screens);
        screens.Changed += (_, _) => watch.ScreensChanged();
        projection.OutputScreenLost += (_, name) => Warn(vm, dispatcher,
            $"Se desconectó la pantalla de la salida ({name}) y la salida se apagó. "
            + "Cuando la vuelvas a enchufar, prendé la salida otra vez.",
            "Se desconectó la pantalla de la salida");
        stage.StageScreenLost += (_, name) => Warn(vm, dispatcher,
            $"Se desconectó la pantalla del escenario ({name}) y el escenario se apagó.",
            "Se desconectó la pantalla del escenario");
    }

    /// <summary>
    /// The list of screens, read again whenever one is plugged in or out.
    /// A chosen screen that went away is remembered: when it comes back it
    /// is chosen again by itself, so projecting is one click as always.
    /// </summary>
    internal sealed class ScreenWatch(MainViewModel vm, IScreenList screens)
    {
        private DisplayInfo? _missingOutput;
        private DisplayInfo? _missingStage;

        public void ScreensChanged()
        {
            var now = screens.GetDisplays();
            if (vm.SelectedDisplay is { BroadcastOnly: false } output && AvaloniaDisplayProvider.Find(output.Info, now) is null)
                _missingOutput = output.Info;
            if (vm.SelectedStageDisplay is { } stage && AvaloniaDisplayProvider.Find(stage.Info, now) is null)
                _missingStage = stage.Info;

            vm.RefreshDisplaysCommand.Execute(null);

            if (_missingOutput is { } wanted && vm.Displays.FirstOrDefault(d => !d.BroadcastOnly
                    && AvaloniaDisplayProvider.Find(wanted, [d.Info]) is not null) is { } back)
            {
                vm.SelectedDisplay = back;
                _missingOutput = null;
            }

            if (_missingStage is { } stageWanted && vm.StageDisplays.FirstOrDefault(d =>
                    AvaloniaDisplayProvider.Find(stageWanted, [d.Info]) is not null) is { } stageBack)
            {
                vm.SelectedStageDisplay = stageBack;
                _missingStage = null;
            }
        }
    }

    private static void Warn(MainViewModel vm, IUiDispatcher dispatcher, string message, string title)
    {
        vm.StatusText = message;
        // After the screen change has been dealt with, not in the middle of it.
        dispatcher.Post(() => Notice.Show(message, title, NoticeKind.Warning));
    }
}
