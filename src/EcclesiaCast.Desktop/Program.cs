using System.Runtime.InteropServices;
using Avalonia;
using Serilog;

namespace EcclesiaCast.Desktop;

/// <summary>
/// The Avalonia program: for now the test program of the Mac plan (the
/// operator's window is provisional, batch 19 brings the real one). The log
/// goes to the data folder, as on Windows: logs/ecclesiacast-AAAAMMDD.log.
/// </summary>
internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var paths = DesktopPlatform.SystemPaths;
        Directory.CreateDirectory(Path.Combine(paths.DataFolder, "logs"));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(paths.DataFolder, "logs", "ecclesiacast-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Error sin atrapar (el programa se cierra: {Terminating})", e.IsTerminating);
            Log.CloseAndFlush();
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Error en una tarea");
            e.SetObserved();
        };

        Log.Information("EcclesiaCast (Avalonia, programa de prueba) arranca, versión {Version}",
            EcclesiaCast.Presentation.Services.AppInfo.Version);
        Log.Information("Sistema: {Os} ({Arch}), .NET {Runtime}",
            RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture, Environment.Version);
        Log.Information("Carpeta de datos: {Folder}; programa en {Base}; lo que viene con el programa en {Bundled}",
            paths.DataFolder, AppContext.BaseDirectory, paths.BundledFolder);

        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "El programa se cerró por un error");
            return 1;
        }
        finally
        {
            Log.Information("EcclesiaCast se cierra");
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
