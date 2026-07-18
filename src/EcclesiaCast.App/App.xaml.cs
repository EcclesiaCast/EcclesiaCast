using System.IO;
using System.Windows;
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

        var appDataDir = Path.Combine(
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
        Log.CloseAndFlush();
        _services?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Convenience for windows/services that optionally use video.</summary>
    public static LibVLC? VideoEngine { get; internal set; }
}
