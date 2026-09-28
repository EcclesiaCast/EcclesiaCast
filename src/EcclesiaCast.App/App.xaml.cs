using System.IO;
using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Data.Persistence;
using LibVLCSharp.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EcclesiaCast.App;

public partial class App : Application
{
    private ServiceProvider? _services;

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
        var appDataDir = Environment.GetEnvironmentVariable("ECCLESIACAST_DATA_DIR") is { Length: > 0 } dataDir
            ? dataDir
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EcclesiaCast");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(appDataDir, "logs", "ecclesiacast-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        Log.Information("EcclesiaCast starting");

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception");
            MessageBox.Show(
                $"Ocurrió un error inesperado:\n\n{args.Exception.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
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
            MessageBox.Show(
                "Se restauró la copia de seguridad.\n\n"
                + "Por las dudas, la biblioteca que tenías antes quedó guardada en:\n\n"
                + DatabaseBackup.ReplacedLibraryPath(dbPath),
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (restore.WasStaged)
        {
            Log.Error("No se pudo aplicar la restauración pendiente: {Error}", restore.Error);
            MessageBox.Show(
                "No se pudo restaurar la copia: algo está usando la biblioteca.\n\n"
                + "Suele ser otra ventana de EcclesiaCast abierta. Cerralas todas y volvé a abrir el "
                + "programa; la copia sigue esperando y se restaura sola.\n\n"
                + $"Detalle: {restore.Error}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        using (var db = new AppDbContext(dbPath))
            db.Database.Migrate();

        // Temas iniciales (Canciones y Biblia) en la primera ejecución.
        ThemeSeeder.EnsureDefaults(new ThemeRepository(dbPath), new SqliteSettingsStore(dbPath));

        // Motor de video (VLC) para los fondos en movimiento.
        LibVLC? libVlc = null;
        try
        {
            LibVLCSharp.Shared.Core.Initialize();
            libVlc = new LibVLC("--no-osd", "--quiet");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo inicializar LibVLC; los videos de fondo quedan deshabilitados");
        }

        VideoEngine = libVlc;

        var services = new ServiceCollection();
        services.AddSingleton<IDisplayProvider, ScreenDisplayProvider>();
        services.AddSingleton<IPresentationService, PresentationService>();
        services.AddSingleton<ProjectionViewModel>();
        services.AddSingleton<IProjectionWindowService, ProjectionWindowService>();
        services.AddSingleton<ISettingsStore>(_ => new SqliteSettingsStore(dbPath));
        services.AddSingleton<ISongRepository>(_ => new SongRepository(dbPath));
        services.AddSingleton<ISongEditor, SongEditorService>();
        services.AddSingleton<IBibleRepository>(_ => new BibleRepository(dbPath));
        services.AddSingleton<IBibleImportDialog, BibleImportDialogService>();
        services.AddSingleton<ITextPrompt, TextPromptService>();
        services.AddSingleton<IThemeRepository>(_ => new ThemeRepository(dbPath));
        services.AddSingleton<IThemeManagerDialog, ThemeManagerDialogService>();
        services.AddSingleton<ISongDesigner, SongDesignerService>();
        services.AddSingleton<IQuickTextEditor, QuickTextEditorService>();
        services.AddSingleton<IMediaRepository>(_ => new MediaRepository(dbPath));
        services.AddSingleton<IMediaInspector, MediaInspectorService>();
        services.AddSingleton<IPlaylistRepository>(_ => new PlaylistRepository(dbPath));
        services.AddSingleton<IYouTubeBrowser, YouTubeBrowserService>();
        services.AddSingleton<ILogoRepository>(_ => new LogoRepository(dbPath));
        services.AddSingleton<ILogoManagerDialog, LogoManagerDialogService>();
        services.AddSingleton<IProPresenterImportDialog, ProPresenterImportDialogService>();
        services.AddSingleton<IStageWindowService, StageWindowService>();
        services.AddSingleton<IBackupDialog>(_ => new BackupDialogService(dbPath));
        services.AddSingleton<ICountdownDialog, CountdownDialogService>();
        services.AddSingleton<ISmartPlaylistDialog, SmartPlaylistDialogService>();
        services.AddSingleton<IGlobalSearchDialog, GlobalSearchDialogService>();
        services.AddSingleton<MainViewModel>();
        _services = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainViewModel>()
        };
        mainWindow.AttachLayoutPersistence(_services.GetRequiredService<ISettingsStore>());
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("EcclesiaCast exiting");

        // Stop listening on the network before the process goes away.
        _services?.GetService<MainViewModel>()?.StopRemote();
        Log.CloseAndFlush();
        _services?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Convenience for windows/services that optionally use video.</summary>
    public static LibVLC? VideoEngine { get; internal set; }
}
