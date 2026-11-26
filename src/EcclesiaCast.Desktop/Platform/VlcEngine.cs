using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using Serilog;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>
/// Starts LibVLC once for the Avalonia interface, saying in the log where it
/// came from: the prototype's <c>VideoEngine</c> (batch 1), with the options
/// of the WPF program. Null when VLC cannot start: the videos stay off and
/// the rest works.
/// </summary>
public static class VlcEngine
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

            // --no-volume-save: VLC must not keep a volume or a mute of its own
            // between runs (see PlayerSound).
            var engine = new LibVLC("--no-osd", "--quiet", "--no-volume-save");
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
