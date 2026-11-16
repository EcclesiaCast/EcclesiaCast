using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Serilog;

namespace EcclesiaCast.Proto;

internal static class Program
{
    /// <summary>When the process started, for «cuánto tarda en abrir».</summary>
    public static readonly Stopwatch SinceStart = Stopwatch.StartNew();

    [STAThread]
    public static int Main(string[] args)
    {
        var folder = ProtoData.DefaultFolder();
        Directory.CreateDirectory(Path.Combine(folder, "logs"));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(folder, "logs", "proto-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Error sin atrapar");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Error en una tarea");
            e.SetObserved();
        };

        Log.Information("── EcclesiaCast Proto arranca ──");
        Log.Information("Sistema: {Os} ({Arch}), .NET {Runtime}",
            RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture, Environment.Version);
        Log.Information("Carpeta de datos: {Folder}", folder);
        Log.Information("Para comparar: ApplicationData = {Roaming}; LocalApplicationData = {Local}",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        Log.Information("Programa en: {Base}", AppContext.BaseDirectory);

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
            Log.Information("── EcclesiaCast Proto se cierra ──");
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
