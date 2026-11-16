using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>Starts LibVLC once, saying in the log where it came from.</summary>
public static class VideoEngine
{
    public static LibVLC? Start()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                // VideoLAN.LibVLC.Mac puts the Apple-chip build under
                // libvlc/osx-arm64/lib next to the program (Contents/MacOS in
                // the bundle), with the plugins in vlc/plugins below it.
                var rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
                var lib = Path.Combine(AppContext.BaseDirectory, "libvlc", rid, "lib");
                var plugins = Path.Combine(lib, "vlc", "plugins");
                Log.Information("LibVLC para Mac: {Lib} (existe: {Exists}); plugins: {Plugins} (existe: {PluginsExist})",
                    lib, File.Exists(Path.Combine(lib, "libvlc.dylib")), plugins, Directory.Exists(plugins));

                Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", plugins);
                LibVLCSharp.Shared.Core.Initialize(lib);
            }
            else
            {
                LibVLCSharp.Shared.Core.Initialize();
            }

            var engine = new LibVLC("--no-osd", "--no-volume-save");
            var warnings = 0;
            engine.Log += (_, e) =>
            {
                // VLC's errors always; its warnings only the first 100, since
                // some videos repeat one every frame.
                if (e.Level == LogLevel.Error)
                    Log.Warning("VLC ({Module}): {Message}", e.Module, e.Message);
                else if (e.Level == LogLevel.Warning && Interlocked.Increment(ref warnings) <= 100)
                    Log.Debug("VLC aviso ({Module}): {Message}", e.Module, e.Message);
            };
            Log.Information("LibVLC {Version} listo", engine.Version);
            return engine;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo iniciar LibVLC; sin video de fondo");
            return null;
        }
    }
}
