using System.IO;
using System.Windows;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Data.Persistence;
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

        var services = new ServiceCollection();
        services.AddSingleton<IDisplayProvider, ScreenDisplayProvider>();
        services.AddSingleton<IPresentationService, PresentationService>();
        services.AddSingleton<ProjectionViewModel>();
        services.AddSingleton<IProjectionWindowService, ProjectionWindowService>();
        services.AddSingleton<ISettingsStore>(_ => new SqliteSettingsStore(dbPath));
        services.AddSingleton<ISongRepository>(_ => new SongRepository(dbPath));
        services.AddSingleton<ISongEditor, SongEditorService>();
        services.AddSingleton<MainViewModel>();
        _services = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainViewModel>()
        };
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
}
